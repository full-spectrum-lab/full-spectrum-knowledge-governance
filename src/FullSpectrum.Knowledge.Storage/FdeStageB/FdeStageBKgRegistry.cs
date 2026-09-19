using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Contracts.FdeStageB;

namespace FullSpectrum.Knowledge.Storage.FdeStageB;

public sealed class FdeStageBKgRegistry : IDisposable
{
    private readonly SqliteDatabase database;
    private readonly FdeKgFailurePoint failurePoint;

    public FdeStageBKgRegistry(string databasePath, FdeKgFailurePoint failurePoint = FdeKgFailurePoint.None)
    {
        this.failurePoint = failurePoint;
        database = new SqliteDatabase(databasePath);
        database.ExecuteScript("""
            CREATE TABLE IF NOT EXISTS kg_fde_stage_b_events (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                event_id TEXT NOT NULL UNIQUE,
                run_id TEXT NOT NULL,
                event_type TEXT NOT NULL,
                occurred_at TEXT NOT NULL,
                payload TEXT NOT NULL,
                payload_sha256 TEXT NOT NULL,
                idempotency_key TEXT NULL,
                copy_id TEXT NULL,
                source_global_ref TEXT NULL,
                resulting_state TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_kg_fde_stage_b_receipt_business_key
              ON kg_fde_stage_b_events(idempotency_key)
              WHERE event_type IN ('ACTION_RECEIPT_DIRECT','ACTION_RECEIPT_RECONCILED');
            CREATE UNIQUE INDEX IF NOT EXISTS ux_kg_fde_stage_b_copy_id
              ON kg_fde_stage_b_events(copy_id)
              WHERE copy_id IS NOT NULL;
            CREATE INDEX IF NOT EXISTS ix_kg_fde_stage_b_run
              ON kg_fde_stage_b_events(run_id, sequence);
            """);
    }

    public T Append<T>(T value) where T : FdeKgEvent
    {
        try
        {
            Validate(value);
            var payload = Canonical(value);
            var expectedDigest = ComputePayloadSha256(value);
            RequireEqual(expectedDigest, value.PayloadSha256, "PAYLOAD_DIGEST_MISMATCH");

            var existing = FindByEventId(value.EventId);
            if (existing is not null)
            {
                if (existing.Value.Type == value.EventType && existing.Value.Payload == payload) return value;
                throw Reject("EVENT_ID_CONFLICT", CurrentState(value), "event_id already identifies different bytes.");
            }

            var (businessKey, copyId, sourceRef, state) = IndexFields(value);
            return database.Transaction(() =>
            {
                if (value is FdeEnginePreActionAudit && failurePoint == FdeKgFailurePoint.BeforePreActionAuditCommit)
                    throw Reject("KG_PRE_ACTION_AUDIT_WRITE", "FAILED_UNCLOSED", "Injected pre-action audit write failure.");
                if (value is FdeActionReceiptDirect or FdeActionReceiptReconciled && failurePoint == FdeKgFailurePoint.BeforeReceiptCommit)
                    throw Reject("KG_RECEIPT_WRITE", "RECONCILIATION_REQUIRED", "Injected receipt write failure.");

                database.Execute("""
                    INSERT INTO kg_fde_stage_b_events(
                      event_id, run_id, event_type, occurred_at, payload, payload_sha256,
                      idempotency_key, copy_id, source_global_ref, resulting_state)
                    VALUES(?,?,?,?,?,?,?,?,?,?);
                    """,
                    value.EventId.ToString(), value.RunId.ToString(), value.EventType, value.OccurredAt.ToString("O"),
                    payload, value.PayloadSha256, businessKey, copyId, sourceRef, state);
                return value;
            });
        }
        catch (FdeKgPersistenceException) { throw; }
        catch (Exception exception)
        {
            var state = value is FdeEnginePreActionAudit ? "FAILED_UNCLOSED" : CurrentState(value);
            throw Reject("KG_PERSISTENCE_FAILED", state, "KG persistence failed closed.", exception);
        }
    }

    public IReadOnlyList<FdeKgEvent> Replay(Guid runId)
    {
        return ReadVerifiedRows()
            .Where(row => row.Event.RunId == runId)
            .Select(row => row.Event)
            .ToArray();
    }

    public FdeKgEvent? Get(string eventGlobalRef)
    {
        if (!eventGlobalRef.StartsWith("kg:", StringComparison.Ordinal)
            || !Guid.TryParse(eventGlobalRef[3..], out var eventId)
            || eventId.Version != 7)
            throw Reject("GLOBAL_REF_INVALID", "FAILED_UNCLOSED", "KG global reference must contain a UUIDv7 local identifier.");

        return ReadVerifiedRows()
            .Where(row => row.Event.EventId == eventId)
            .Select(row => row.Event)
            .SingleOrDefault();
    }

    public FdeKgRevisionRoot RevisionRoot()
    {
        return database.Transaction(() =>
        {
            var rows = ReadVerifiedRows();
            var revision = rows.Count == 0 ? 0 : rows[^1].Sequence;
            var ordered = rows.Select(row => new
            {
                event_global_ref = $"kg:{row.Event.EventId}",
                event_type = row.Event.EventType,
                payload_sha256 = row.Event.PayloadSha256
            }).ToArray();
            var digest = Sha256(DeterministicJson.Canonicalize(JsonSerializer.Serialize(ordered, KnowledgeJson.Options)));
            return new FdeKgRevisionRoot(
                $"kg-revision:{revision}",
                digest,
                ordered.Select(item => item.event_global_ref).ToArray());
        });
    }

    public static string ComputeCopyId(string sourceGlobalRef) => Sha256(sourceGlobalRef);

    public static string ComputePayloadSha256(FdeKgEvent value)
    {
        var element = JsonSerializer.SerializeToElement(value, value.GetType(), KnowledgeJson.Options);
        var fields = element.EnumerateObject()
            .Where(property => !property.Name.Equals("payload_sha256", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(property => property.Name, property => property.Value);
        return Sha256(DeterministicJson.Canonicalize(JsonSerializer.Serialize(fields, KnowledgeJson.Options)));
    }

    private void Validate(FdeKgEvent value)
    {
        if (value.EventId.Version != 7 || value.RunId.Version != 7)
            throw Reject("UUIDV7_REQUIRED", CurrentState(value), "event_id and run_id must be UUIDv7.");
        RequireSha(value.PayloadSha256, nameof(value.PayloadSha256));
        var expectedTypes = value switch
        {
            FdeEnginePreActionAudit => new[] { "ENGINE_COMPUTATION_RECORDED" },
            FdeHumanDecision => new[] { FdeStageBEventTypes.EnginePreActionAudit },
            FdeActionIntent => new[] { FdeStageBEventTypes.HumanDecision },
            FdeActionReceiptDirect => new[] { FdeStageBEventTypes.ActionIntent, "SINK_COMMIT_OBSERVED", "CANONICAL_RECEIPT" },
            FdeActionReceiptReconciled => new[] { FdeStageBEventTypes.ActionIntent, "RECONCILIATION_RESULT", "CANONICAL_RECEIPT" },
            FdeKgFailureCopy => new[] { "PROTOCOL_FAILURE" },
            FdeKgNoActionCopy => new[] { "PROTOCOL_NO_ACTION", FdeStageBEventTypes.KgFailureCopy },
            _ => throw Reject("FORBIDDEN_KG_EVENT_TYPE", CurrentState(value), "KG does not accept this event type.")
        };
        var actualTypes = value.Predecessors.Select(item => item.TargetType).Order().ToArray();
        if (!actualTypes.SequenceEqual(expectedTypes.Order()))
            throw Reject("PREDECESSOR_SET_NOT_EXACT", CurrentState(value), "Predecessor type set is not complete and exact.");
        foreach (var predecessor in value.Predecessors)
        {
            if (!IsGlobalRef(predecessor.TargetGlobalRef) || !IsSha(predecessor.TargetPayloadSha256))
                throw Reject("PREDECESSOR_REFERENCE_INVALID", CurrentState(value), "Predecessor reference or digest is invalid.");
            if (predecessor.TargetGlobalRef.StartsWith("kg:", StringComparison.Ordinal))
            {
                var local = FindByGlobalRef(predecessor.TargetGlobalRef)
                    ?? throw Reject("PREDECESSOR_MISSING", CurrentState(value), "Local KG predecessor is not durable.");
                RequireEqual(local.EventType, predecessor.TargetType, "PREDECESSOR_TYPE_MISMATCH");
                RequireEqual(local.PayloadSha256, predecessor.TargetPayloadSha256, "PREDECESSOR_DIGEST_MISMATCH");
                if (local.RunId != value.RunId) throw Reject("RUN_BINDING_MISMATCH", CurrentState(value), "Local predecessor belongs to another run.");
            }
        }

        switch (value)
        {
            case FdeEnginePreActionAudit audit:
                RequireEqual("SUCCESS", audit.AuditStatus, "AUDIT_STATUS_INVALID");
                RequireSha(audit.EngineResultSha256, nameof(audit.EngineResultSha256));
                break;
            case FdeHumanDecision decision:
                if (decision.Decision is not ("APPROVE" or "REJECT"))
                    throw Reject("DECISION_INVALID", CurrentState(value), "Decision must be APPROVE or REJECT.");
                RequireText(decision.ActorId, nameof(decision.ActorId));
                break;
            case FdeActionIntent intent:
                RequireText(intent.ActionType, nameof(intent.ActionType));
                RequireSha(intent.ActionInputSha256, nameof(intent.ActionInputSha256));
                RequireIdempotencyKey(intent.IdempotencyKey);
                break;
            case FdeActionReceiptDirect direct:
                ValidateReceipt(direct.IdempotencyKey, direct.ReceiptJson, direct.ReceiptSha256, direct.ActionInputSha256, direct.ConfirmationMode, "DIRECT");
                break;
            case FdeActionReceiptReconciled reconciled:
                ValidateReceipt(reconciled.IdempotencyKey, reconciled.ReceiptJson, reconciled.ReceiptSha256, reconciled.ActionInputSha256, reconciled.ConfirmationMode, "RECONCILED");
                if (reconciled.QueryId.Version != 7) throw Reject("UUIDV7_REQUIRED", "RECONCILIATION_REQUIRED", "query_id must be UUIDv7.");
                break;
            case FdeKgFailureCopy failure:
                ValidateCopy(failure.SourceSystem, failure.SourceGlobalRef, failure.SourceEventType, failure.SourcePayloadSha256, failure.CopyId);
                RequireEqual("PROTOCOL_FAILURE", failure.SourceEventType, "COPY_SOURCE_TYPE_INVALID");
                RequireText(failure.ErrorCode, nameof(failure.ErrorCode));
                var expectedState = failure.FailureStage switch
                {
                    "KG_PRE_ACTION_AUDIT_WRITE" => "FAILED_UNCLOSED",
                    "KG_RECEIPT_WRITE" => "RECONCILIATION_REQUIRED",
                    _ => throw Reject("FAILURE_STAGE_INVALID", failure.ResultingState, "Unknown KG failure stage.")
                };
                RequireEqual(expectedState, failure.ResultingState, "FAILURE_STATE_INVALID");
                break;
            case FdeKgNoActionCopy noAction:
                ValidateCopy(noAction.SourceSystem, noAction.SourceGlobalRef, noAction.SourceEventType, noAction.SourcePayloadSha256, noAction.CopyId);
                if (noAction.SourceEventType != "PROTOCOL_NO_ACTION") throw Reject("COPY_SOURCE_TYPE_INVALID", "FAILED_UNCLOSED", "NO_ACTION copy requires PROTOCOL_NO_ACTION.");
                RequireText(noAction.OutcomeStage, nameof(noAction.OutcomeStage));
                RequireText(noAction.ReasonCode, nameof(noAction.ReasonCode));
                var failureCopy = FindByGlobalRef(noAction.CopyPredecessorGlobalRef);
                if (failureCopy is not FdeKgFailureCopy { FailureStage: "KG_PRE_ACTION_AUDIT_WRITE" })
                    throw Reject("NO_ACTION_COPY_FORBIDDEN", CurrentState(value), "NO_ACTION copy requires a durable pre-action failure copy in the same run.");
                if (failureCopy.RunId != noAction.RunId) throw Reject("RUN_BINDING_MISMATCH", CurrentState(value), "Copy predecessor belongs to another run.");
                break;
        }
    }

    private static void ValidateReceipt(string key, string receiptJson, string receiptSha, string inputSha, string mode, string expectedMode)
    {
        RequireIdempotencyKey(key);
        RequireSha(receiptSha, nameof(receiptSha)); RequireSha(inputSha, nameof(inputSha));
        RequireEqual(Sha256(DeterministicJson.Canonicalize(receiptJson)), receiptSha, "RECEIPT_DIGEST_MISMATCH");
        RequireEqual(expectedMode, mode, "CONFIRMATION_MODE_INVALID");
    }

    private static void ValidateCopy(string system, string sourceRef, string sourceType, string sourceSha, string copyId)
    {
        RequireEqual("PROTOCOL", system, "COPY_SOURCE_SYSTEM_INVALID");
        if (!sourceRef.StartsWith("protocol:", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(sourceType))
            throw Reject("COPY_SOURCE_REFERENCE_INVALID", "FAILED_UNCLOSED", "Copy source must be a Protocol global reference.");
        RequireSha(sourceSha, nameof(sourceSha));
        RequireEqual(ComputeCopyId(sourceRef), copyId, "COPY_ID_INVALID");
    }

    private (string Type, string Payload)? FindByEventId(Guid id)
    {
        var rows = database.Query("SELECT event_type,payload FROM kg_fde_stage_b_events WHERE event_id=?;",
            row => (Type: row.Text(0), Payload: row.Text(1)), id.ToString());
        return rows.Count == 0 ? null : rows.Single();
    }

    private FdeKgEvent? FindByGlobalRef(string globalRef)
    {
        if (!globalRef.StartsWith("kg:", StringComparison.Ordinal) || !Guid.TryParse(globalRef[3..], out var id)) return null;
        var row = database.Query("SELECT event_type,payload FROM kg_fde_stage_b_events WHERE event_id=?;",
            item => (Type: item.Text(0), Payload: item.Text(1)), id.ToString()).SingleOrDefault();
        return row == default ? null : Deserialize(row.Type, row.Payload);
    }

    private IReadOnlyList<VerifiedRow> ReadVerifiedRows()
    {
        var rows = database.Query(
            "SELECT sequence,event_id,run_id,event_type,occurred_at,payload,payload_sha256 FROM kg_fde_stage_b_events ORDER BY sequence;",
            row => new PersistedRow(
                row.Int64(0), row.Text(1), row.Text(2), row.Text(3), row.Text(4), row.Text(5), row.Text(6)));
        var verified = new List<VerifiedRow>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            if (row.Sequence != index + 1)
                throw Reject("PERSISTED_SEQUENCE_INVALID", "FAILED_UNCLOSED", "KG durable sequence must be contiguous and start at one.");
            var value = Deserialize(row.EventType, row.Payload);
            RequireEqual(row.EventId, value.EventId.ToString(), "PERSISTED_EVENT_ID_COLUMN_MISMATCH");
            RequireEqual(row.RunId, value.RunId.ToString(), "PERSISTED_RUN_ID_COLUMN_MISMATCH");
            RequireEqual(row.EventType, value.EventType, "PERSISTED_EVENT_TYPE_COLUMN_MISMATCH");
            RequireEqual(row.OccurredAt, value.OccurredAt.ToString("O"), "PERSISTED_OCCURRED_AT_COLUMN_MISMATCH");
            RequireEqual(row.PayloadSha256, value.PayloadSha256, "PERSISTED_DIGEST_COLUMN_MISMATCH");
            RequireEqual(ComputePayloadSha256(value), value.PayloadSha256, "PERSISTED_PAYLOAD_TAMPERED");
            Validate(value);
            verified.Add(new VerifiedRow(row.Sequence, value));
        }
        return verified;
    }

    private static (string? Key, string? Copy, string? Source, string? State) IndexFields(FdeKgEvent value) => value switch
    {
        FdeActionReceiptDirect receipt => (receipt.IdempotencyKey, null, null, null),
        FdeActionReceiptReconciled receipt => (receipt.IdempotencyKey, null, null, null),
        FdeKgFailureCopy copy => (null, copy.CopyId, copy.SourceGlobalRef, copy.ResultingState),
        FdeKgNoActionCopy copy => (null, copy.CopyId, copy.SourceGlobalRef, "FAILED_UNCLOSED"),
        _ => (null, null, null, null)
    };

    private static string CurrentState(FdeKgEvent value) => value switch
    {
        FdeKgFailureCopy copy => copy.ResultingState,
        FdeActionReceiptDirect or FdeActionReceiptReconciled => "RECONCILIATION_REQUIRED",
        _ => "FAILED_UNCLOSED"
    };

    private static FdeKgEvent Deserialize(string type, string json) => type switch
    {
        FdeStageBEventTypes.EnginePreActionAudit => JsonSerializer.Deserialize<FdeEnginePreActionAudit>(json, KnowledgeJson.Options)!,
        FdeStageBEventTypes.HumanDecision => JsonSerializer.Deserialize<FdeHumanDecision>(json, KnowledgeJson.Options)!,
        FdeStageBEventTypes.ActionIntent => JsonSerializer.Deserialize<FdeActionIntent>(json, KnowledgeJson.Options)!,
        FdeStageBEventTypes.ActionReceiptDirect => JsonSerializer.Deserialize<FdeActionReceiptDirect>(json, KnowledgeJson.Options)!,
        FdeStageBEventTypes.ActionReceiptReconciled => JsonSerializer.Deserialize<FdeActionReceiptReconciled>(json, KnowledgeJson.Options)!,
        FdeStageBEventTypes.KgFailureCopy => JsonSerializer.Deserialize<FdeKgFailureCopy>(json, KnowledgeJson.Options)!,
        FdeStageBEventTypes.KgNoActionCopy => JsonSerializer.Deserialize<FdeKgNoActionCopy>(json, KnowledgeJson.Options)!,
        _ => throw Reject("FORBIDDEN_KG_EVENT_TYPE", "FAILED_UNCLOSED", "Persisted KG event type is forbidden.")
    };

    private static string Canonical(object value) => DeterministicJson.Canonicalize(JsonSerializer.Serialize(value, value.GetType(), KnowledgeJson.Options));
    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool IsSha(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
    private static bool IsGlobalRef(string value) => value.Split(':') is ["protocol" or "kg" or "sink", var id] && Guid.TryParse(id, out var parsed) && parsed.Version == 7;
    private static void RequireText(string value, string field) { if (string.IsNullOrWhiteSpace(value)) throw Reject("REQUIRED_TEXT_INVALID", "FAILED_UNCLOSED", $"{field} must be non-empty."); }
    private static void RequireIdempotencyKey(string value) { RequireText(value, "idempotency_key"); if (value.Length > 256) throw Reject("IDEMPOTENCY_KEY_TOO_LONG", "RECONCILIATION_REQUIRED", "idempotency_key exceeds 256 characters."); }
    private static void RequireSha(string value, string field) { if (!IsSha(value)) throw Reject("SHA256_INVALID", "FAILED_UNCLOSED", $"{field} must be SHA-256."); }
    private static void RequireEqual(string expected, string actual, string code) { if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)) throw Reject(code, "FAILED_UNCLOSED", $"Expected {expected}, got {actual}."); }
    private static FdeKgPersistenceException Reject(string code, string state, string message, Exception? inner = null) => new(code, state, message, inner);
    private sealed record PersistedRow(long Sequence, string EventId, string RunId, string EventType, string OccurredAt, string Payload, string PayloadSha256);
    private sealed record VerifiedRow(long Sequence, FdeKgEvent Event);
    public void Dispose() => database.Dispose();
}
