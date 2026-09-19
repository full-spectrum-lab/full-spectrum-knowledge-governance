using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Contracts.FdeStageB;
using FullSpectrum.Knowledge.StageBPort;
using FullSpectrum.Knowledge.Storage.FdeStageB;

namespace FullSpectrum.Knowledge.Tests;

internal static class FdeStageBKgTests
{
    internal static IReadOnlyList<(string Name, Action Test)> Cases =>
    [
        ("stage B KG persists direct receipt and replays after reopen", DirectReceiptReplay),
        ("stage B KG receipts are mutually exclusive by business key", ReceiptCrossTypeUniqueness),
        ("stage B KG receipt failure stays reconciliation required", ReceiptFailureState),
        ("stage B KG pre-action audit failure is atomic and failed unclosed", AuditFailureAtomicity),
        ("stage B KG copy id is exactly SHA256 source global ref", CopyIdRule),
        ("stage B KG failure copy is at most one", FailureCopyCardinality),
        ("stage B KG forbids no-action copy after receipt failure", ReceiptFailureForbidsNoActionCopy),
        ("stage B KG rejects incomplete receipt predecessors", ReceiptPredecessorSet),
        ("stage B KG enforces frozen schema field constraints", FrozenSchemaFieldConstraints),
        ("stage B KG replay rejects persisted payload tampering", ReplayTamperFailsClosed),
        ("stage B KG revision root is stable across close and reopen", RevisionRootStableAcrossReopen),
        ("stage B KG revision root changes after a durable append", RevisionRootChangesAfterAppend),
        ("stage B KG revision root rejects persisted index tampering", RevisionRootRejectsIndexTampering),
        ("stage B KG get rejects persisted event id index tampering", GetRejectsEventIdIndexTampering),
        ("stage B KG replay rejects persisted run id index tampering", ReplayRejectsRunIdIndexTampering),
        ("stage B KG revision root rejects durable sequence tampering", RevisionRootRejectsSequenceTampering),
        ("stage B KG public get returns immutable authority objects", PublicGet),
        ("stage B KG JSON Lines port binds requests and survives reopen", JsonLinesPort)
    ];

    private static void DirectReceiptReplay()
    {
        using var fixture = new Fixture();
        var (_, _, intent) = fixture.AppendApprovedIntent();
        var receipt = fixture.Direct(intent, "idem-direct");
        fixture.Registry.Append(receipt);
        fixture.Reopen();
        var replay = fixture.Registry.Replay(fixture.RunId);
        Equal(4, replay.Count);
        Equal(FdeStageBEventTypes.ActionReceiptDirect, replay[^1].EventType);
    }

    private static void ReceiptCrossTypeUniqueness()
    {
        using var fixture = new Fixture();
        var (_, _, intent) = fixture.AppendApprovedIntent();
        fixture.Registry.Append(fixture.Direct(intent, "idem-shared"));
        var reconciled = fixture.Reconciled(intent, "idem-shared");
        Throws("KG_PERSISTENCE_FAILED", () => fixture.Registry.Append(reconciled));
        Equal(4, fixture.Registry.Replay(fixture.RunId).Count);
    }

    private static void ReceiptFailureState()
    {
        using var fixture = new Fixture(FdeKgFailurePoint.BeforeReceiptCommit);
        var (_, _, intent) = fixture.AppendApprovedIntent();
        var error = Throws("KG_RECEIPT_WRITE", () => fixture.Registry.Append(fixture.Direct(intent, "idem-fail")));
        Equal("RECONCILIATION_REQUIRED", error.ResultingState);
        False(error.RetryAllowed);
        Equal(3, fixture.Registry.Replay(fixture.RunId).Count);
    }

    private static void AuditFailureAtomicity()
    {
        using var fixture = new Fixture(FdeKgFailurePoint.BeforePreActionAuditCommit);
        var error = Throws("KG_PRE_ACTION_AUDIT_WRITE", () => fixture.Registry.Append(fixture.Audit()));
        Equal("FAILED_UNCLOSED", error.ResultingState);
        Equal(0, fixture.Registry.Replay(fixture.RunId).Count);
    }

    private static void CopyIdRule()
    {
        using var fixture = new Fixture();
        var sourceRef = $"protocol:{Guid.CreateVersion7()}";
        var copy = fixture.FailureCopy(sourceRef, "KG_RECEIPT_WRITE", "RECONCILIATION_REQUIRED");
        Equal(FdeStageBKgRegistry.ComputeCopyId(sourceRef), copy.CopyId);
        fixture.Registry.Append(copy);
        var invalid = fixture.FailureCopy($"protocol:{Guid.CreateVersion7()}", "KG_PRE_ACTION_AUDIT_WRITE", "FAILED_UNCLOSED") with { CopyId = Sha('9') };
        invalid = WithDigest(invalid);
        Throws("COPY_ID_INVALID", () => fixture.Registry.Append(invalid));
    }

    private static void FailureCopyCardinality()
    {
        using var fixture = new Fixture();
        var sourceRef = $"protocol:{Guid.CreateVersion7()}";
        fixture.Registry.Append(fixture.FailureCopy(sourceRef, "KG_RECEIPT_WRITE", "RECONCILIATION_REQUIRED"));
        var duplicate = fixture.FailureCopy(sourceRef, "KG_RECEIPT_WRITE", "RECONCILIATION_REQUIRED");
        Throws("KG_PERSISTENCE_FAILED", () => fixture.Registry.Append(duplicate));
        Equal(1, fixture.Registry.Replay(fixture.RunId).Count);
    }

    private static void ReceiptFailureForbidsNoActionCopy()
    {
        using var fixture = new Fixture();
        var failure = fixture.FailureCopy($"protocol:{Guid.CreateVersion7()}", "KG_RECEIPT_WRITE", "RECONCILIATION_REQUIRED");
        fixture.Registry.Append(failure);
        var noAction = fixture.NoActionCopy(failure);
        Throws("NO_ACTION_COPY_FORBIDDEN", () => fixture.Registry.Append(noAction));
    }

    private static void ReceiptPredecessorSet()
    {
        using var fixture = new Fixture();
        var (_, _, intent) = fixture.AppendApprovedIntent();
        var receipt = fixture.Direct(intent, "idem-preds") with { Predecessors = [fixture.Ref(intent)] };
        receipt = WithDigest(receipt);
        Throws("PREDECESSOR_SET_NOT_EXACT", () => fixture.Registry.Append(receipt));
    }

    private static void ReplayTamperFailsClosed()
    {
        using var fixture = new Fixture();
        fixture.Registry.Append(fixture.Audit());
        fixture.ExecuteSql("UPDATE kg_fde_stage_b_events SET payload=replace(payload, 'SUCCESS', 'TAMPERED');");
        Throws("PERSISTED_PAYLOAD_TAMPERED", () => fixture.Registry.Replay(fixture.RunId));
    }

    private static void FrozenSchemaFieldConstraints()
    {
        using var fixture = new Fixture();
        var audit = fixture.Audit();
        Throws("AUDIT_STATUS_INVALID", () => fixture.Registry.Append(WithDigest(audit with { AuditStatus = "FAILED" })));
        Throws("SHA256_INVALID", () => fixture.Registry.Append(WithDigest(audit with { EventId = Guid.CreateVersion7(), EngineResultSha256 = "bad" })));
        audit = fixture.Registry.Append(audit);

        var decision = fixture.Decision(audit);
        Throws("DECISION_INVALID", () => fixture.Registry.Append(WithDigest(decision with { Decision = "MAYBE" })));
        Throws("REQUIRED_TEXT_INVALID", () => fixture.Registry.Append(WithDigest(decision with { EventId = Guid.CreateVersion7(), ActorId = " " })));
        decision = fixture.Registry.Append(decision);

        var intent = fixture.Intent(decision);
        Throws("REQUIRED_TEXT_INVALID", () => fixture.Registry.Append(WithDigest(intent with { ActionType = "" })));
        Throws("SHA256_INVALID", () => fixture.Registry.Append(WithDigest(intent with { EventId = Guid.CreateVersion7(), ActionInputSha256 = "bad" })));
        Throws("IDEMPOTENCY_KEY_TOO_LONG", () => fixture.Registry.Append(WithDigest(intent with { EventId = Guid.CreateVersion7(), IdempotencyKey = new string('x', 257) })));

        var failure = fixture.FailureCopy($"protocol:{Guid.CreateVersion7()}", "KG_RECEIPT_WRITE", "RECONCILIATION_REQUIRED");
        Throws("COPY_SOURCE_TYPE_INVALID", () => fixture.Registry.Append(WithDigest(failure with { SourceEventType = "OTHER" })));
        Throws("REQUIRED_TEXT_INVALID", () => fixture.Registry.Append(WithDigest(failure with { EventId = Guid.CreateVersion7(), ErrorCode = "" })));

        var preActionFailure = fixture.FailureCopy($"protocol:{Guid.CreateVersion7()}", "KG_PRE_ACTION_AUDIT_WRITE", "FAILED_UNCLOSED");
        fixture.Registry.Append(preActionFailure);
        var noAction = fixture.NoActionCopy(preActionFailure);
        Throws("REQUIRED_TEXT_INVALID", () => fixture.Registry.Append(WithDigest(noAction with { OutcomeStage = "" })));
        Throws("REQUIRED_TEXT_INVALID", () => fixture.Registry.Append(WithDigest(noAction with { EventId = Guid.CreateVersion7(), ReasonCode = " " })));
    }

    private static void RevisionRootStableAcrossReopen()
    {
        using var fixture = new Fixture();
        var audit = fixture.Registry.Append(fixture.Audit());
        var before = fixture.Registry.RevisionRoot();
        fixture.Reopen();
        var after = fixture.Registry.RevisionRoot();
        Equal(before.KgRevisionRef, after.KgRevisionRef);
        Equal(before.KgRootSha256, after.KgRootSha256);
        True(before.OrderedEventRefs.SequenceEqual(after.OrderedEventRefs));
        Equal("kg-revision:1", after.KgRevisionRef);
        Equal($"kg:{audit.EventId}", after.OrderedEventRefs.Single());
    }

    private static void RevisionRootChangesAfterAppend()
    {
        using var fixture = new Fixture();
        var audit = fixture.Registry.Append(fixture.Audit());
        var first = fixture.Registry.RevisionRoot();
        fixture.Registry.Append(fixture.Decision(audit));
        var second = fixture.Registry.RevisionRoot();
        Equal("kg-revision:2", second.KgRevisionRef);
        False(string.Equals(first.KgRootSha256, second.KgRootSha256, StringComparison.OrdinalIgnoreCase));
        Equal(2, second.OrderedEventRefs.Count);
    }

    private static void RevisionRootRejectsIndexTampering()
    {
        using var fixture = new Fixture();
        fixture.Registry.Append(fixture.Audit());
        fixture.ExecuteSql($"UPDATE kg_fde_stage_b_events SET run_id='{Guid.CreateVersion7()}';");
        Throws("PERSISTED_RUN_ID_COLUMN_MISMATCH", () => fixture.Registry.RevisionRoot());
    }

    private static void GetRejectsEventIdIndexTampering()
    {
        using var fixture = new Fixture();
        var audit = fixture.Registry.Append(fixture.Audit());
        fixture.ExecuteSql($"UPDATE kg_fde_stage_b_events SET event_id='{Guid.CreateVersion7()}';");
        Throws("PERSISTED_EVENT_ID_COLUMN_MISMATCH", () => fixture.Registry.Get($"kg:{audit.EventId}"));
    }

    private static void ReplayRejectsRunIdIndexTampering()
    {
        using var fixture = new Fixture();
        fixture.Registry.Append(fixture.Audit());
        fixture.ExecuteSql($"UPDATE kg_fde_stage_b_events SET run_id='{Guid.CreateVersion7()}';");
        Throws("PERSISTED_RUN_ID_COLUMN_MISMATCH", () => fixture.Registry.Replay(fixture.RunId));
    }

    private static void RevisionRootRejectsSequenceTampering()
    {
        using var fixture = new Fixture();
        fixture.Registry.Append(fixture.Audit());
        fixture.ExecuteSql("UPDATE kg_fde_stage_b_events SET sequence=7 WHERE sequence=1;");
        Throws("PERSISTED_SEQUENCE_INVALID", () => fixture.Registry.RevisionRoot());
    }

    private static void PublicGet()
    {
        using var fixture = new Fixture();
        var audit = fixture.Registry.Append(fixture.Audit());
        var found = fixture.Registry.Get($"kg:{audit.EventId}");
        var foundAudit = found as FdeEnginePreActionAudit
            ?? throw new InvalidOperationException("Expected audit event.");
        Equal(
            DeterministicJson.Canonicalize(JsonSerializer.Serialize(audit, KnowledgeJson.Options)),
            DeterministicJson.Canonicalize(JsonSerializer.Serialize(foundAudit, KnowledgeJson.Options)));
        Equal<FdeKgEvent?>(null, fixture.Registry.Get($"kg:{Guid.CreateVersion7()}"));
        Throws("GLOBAL_REF_INVALID", () => fixture.Registry.Get("kg:not-a-uuid"));
    }

    private static void JsonLinesPort()
    {
        using var fixture = new Fixture(openRegistry: false);
        using var session = new FdeStageBPortSession(fixture.DatabasePath);
        var audit = fixture.Audit();

        using var append = Parse(session.ProcessLine(Request("req-1", "append", new { @event = audit })));
        True(append.RootElement.GetProperty("ok").GetBoolean());
        Equal("req-1", append.RootElement.GetProperty("request_id").GetString());
        Equal($"kg:{audit.EventId}", append.RootElement.GetProperty("result").GetProperty("event_global_ref").GetString());

        using var get = Parse(session.ProcessLine(Request("req-2", "get", new { event_global_ref = $"kg:{audit.EventId}" })));
        Equal("FOUND", get.RootElement.GetProperty("result").GetProperty("status").GetString());

        using var root1 = Parse(session.ProcessLine(Request("req-3", "revision_root", new { })));
        var digest1 = root1.RootElement.GetProperty("result").GetProperty("kg_root_sha256").GetString();
        Equal("kg-revision:1", root1.RootElement.GetProperty("result").GetProperty("kg_revision_ref").GetString());

        using var reopened = Parse(session.ProcessLine(Request("req-4", "close_reopen", new { })));
        Equal("REOPENED", reopened.RootElement.GetProperty("result").GetProperty("status").GetString());
        using var root2 = Parse(session.ProcessLine(Request("req-5", "revision_root", new { })));
        Equal(digest1, root2.RootElement.GetProperty("result").GetProperty("kg_root_sha256").GetString());

        using var duplicate = Parse(session.ProcessLine(Request("req-5", "revision_root", new { })));
        False(duplicate.RootElement.GetProperty("ok").GetBoolean());
        Equal("REQUEST_ID_REUSED", duplicate.RootElement.GetProperty("error").GetProperty("code").GetString());

        using var unknown = Parse(session.ProcessLine(Request("req-6", "unknown", new { })));
        False(unknown.RootElement.GetProperty("ok").GetBoolean());
        Equal("OPERATION_UNKNOWN", unknown.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    private static string Request(string requestId, string operation, object payload) => JsonSerializer.Serialize(new
    {
        protocol_version = FdeStageBPortSession.ProtocolVersion,
        request_id = requestId,
        operation,
        payload
    }, new JsonSerializerOptions(KnowledgeJson.Options) { WriteIndented = false });

    private static JsonDocument Parse(string json)
    {
        if (json.Contains('\n') || json.Contains('\r')) throw new InvalidOperationException("Port response must be exactly one JSON line.");
        return JsonDocument.Parse(json);
    }

    private static T WithDigest<T>(T value) where T : FdeKgEvent
    {
        var digest = FdeStageBKgRegistry.ComputePayloadSha256(value);
        return (T)(object)(value switch
        {
            FdeEnginePreActionAudit item => item with { PayloadSha256 = digest },
            FdeHumanDecision item => item with { PayloadSha256 = digest },
            FdeActionIntent item => item with { PayloadSha256 = digest },
            FdeActionReceiptDirect item => item with { PayloadSha256 = digest },
            FdeActionReceiptReconciled item => item with { PayloadSha256 = digest },
            FdeKgFailureCopy item => item with { PayloadSha256 = digest },
            FdeKgNoActionCopy item => item with { PayloadSha256 = digest },
            _ => throw new InvalidOperationException()
        });
    }

    private static string Sha(char value) => new(value, 64);
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}; got {actual}."); }
    private static void True(bool value) { if (!value) throw new InvalidOperationException("Expected true."); }
    private static void False(bool value) { if (value) throw new InvalidOperationException("Expected false."); }
    private static FdeKgPersistenceException Throws(string code, Action action)
    {
        try { action(); }
        catch (FdeKgPersistenceException error) { Equal(code, error.ErrorCode); return error; }
        throw new InvalidOperationException($"Expected {code}.");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), $"fde-stage-b-kg-{Guid.NewGuid():N}");
        private readonly string path;
        private readonly FdeKgFailurePoint failurePoint;
        internal Guid RunId { get; } = Guid.CreateVersion7();
        internal FdeStageBKgRegistry? OptionalRegistry { get; private set; }
        internal FdeStageBKgRegistry Registry => OptionalRegistry ?? throw new InvalidOperationException("Fixture registry was intentionally not opened.");
        internal string DatabasePath => path;

        internal Fixture(FdeKgFailurePoint failurePoint = FdeKgFailurePoint.None, bool openRegistry = true)
        {
            this.failurePoint = failurePoint;
            Directory.CreateDirectory(root);
            path = Path.Combine(root, "kg.sqlite3");
            if (openRegistry) OptionalRegistry = new FdeStageBKgRegistry(path, failurePoint);
        }

        internal FdeEnginePreActionAudit Audit()
        {
            var value = new FdeEnginePreActionAudit(Guid.CreateVersion7(), RunId, DateTimeOffset.UtcNow, Sha('0'),
                [External("protocol", "ENGINE_COMPUTATION_RECORDED")], Sha('a'), "SUCCESS");
            return WithDigest(value);
        }

        internal (FdeEnginePreActionAudit Audit, FdeHumanDecision Decision, FdeActionIntent Intent) AppendApprovedIntent()
        {
            var audit = Registry.Append(Audit());
            var decision = Registry.Append(Decision(audit));
            var intent = Registry.Append(Intent(decision));
            return (audit, decision, intent);
        }

        internal FdeHumanDecision Decision(FdeEnginePreActionAudit audit) => WithDigest(new FdeHumanDecision(
            Guid.CreateVersion7(), RunId, DateTimeOffset.UtcNow, Sha('0'), [Ref(audit)], "APPROVE", "human", DateTimeOffset.UtcNow));

        internal FdeActionIntent Intent(FdeHumanDecision decision) => WithDigest(new FdeActionIntent(
            Guid.CreateVersion7(), RunId, DateTimeOffset.UtcNow, Sha('0'), [Ref(decision)], "SYNTHETIC_ACTION", Sha('b'), "intent-business-key"));

        internal FdeActionReceiptDirect Direct(FdeActionIntent intent, string key)
        {
            const string receipt = "{\"status\":\"committed\"}";
            var value = new FdeActionReceiptDirect(Guid.CreateVersion7(), RunId, DateTimeOffset.UtcNow, Sha('0'),
                [Ref(intent), External("protocol", "SINK_COMMIT_OBSERVED"), External("sink", "CANONICAL_RECEIPT")],
                key, receipt, FullSpectrum.Knowledge.Contracts.DeterministicJson.ComputeSha256(receipt).Value.ToUpperInvariant(), intent.ActionInputSha256);
            return WithDigest(value);
        }

        internal FdeActionReceiptReconciled Reconciled(FdeActionIntent intent, string key)
        {
            const string receipt = "{\"status\":\"committed\"}";
            var value = new FdeActionReceiptReconciled(Guid.CreateVersion7(), RunId, DateTimeOffset.UtcNow, Sha('0'),
                [Ref(intent), External("protocol", "RECONCILIATION_RESULT"), External("sink", "CANONICAL_RECEIPT")],
                key, receipt, FullSpectrum.Knowledge.Contracts.DeterministicJson.ComputeSha256(receipt).Value.ToUpperInvariant(), intent.ActionInputSha256, Guid.CreateVersion7());
            return WithDigest(value);
        }

        internal FdeKgFailureCopy FailureCopy(string sourceRef, string stage, string state)
        {
            var value = new FdeKgFailureCopy(Guid.CreateVersion7(), RunId, DateTimeOffset.UtcNow, Sha('0'),
                [new(sourceRef, "PROTOCOL_FAILURE", Sha('c'))], "PROTOCOL", sourceRef, "PROTOCOL_FAILURE", Sha('c'),
                FdeStageBKgRegistry.ComputeCopyId(sourceRef), stage, "WRITE_FAILED", state);
            return WithDigest(value);
        }

        internal FdeKgNoActionCopy NoActionCopy(FdeKgFailureCopy failure)
        {
            var sourceRef = $"protocol:{Guid.CreateVersion7()}";
            var value = new FdeKgNoActionCopy(Guid.CreateVersion7(), RunId, DateTimeOffset.UtcNow, Sha('0'),
                [new(sourceRef, "PROTOCOL_NO_ACTION", Sha('d')), Ref(failure)], "PROTOCOL", sourceRef,
                "PROTOCOL_NO_ACTION", Sha('d'), FdeStageBKgRegistry.ComputeCopyId(sourceRef), "FAILURE_CLOSURE", "NO_ACTION",
                $"kg:{failure.EventId}");
            return WithDigest(value);
        }

        internal FdeGlobalEventRef Ref(FdeKgEvent value) => new($"kg:{value.EventId}", value.EventType, value.PayloadSha256);
        private static FdeGlobalEventRef External(string system, string type) => new($"{system}:{Guid.CreateVersion7()}", type, Sha('e'));

        internal void Reopen() { Registry.Dispose(); OptionalRegistry = new FdeStageBKgRegistry(path); }
        internal void ExecuteSql(string sql)
        {
            var field = typeof(FdeStageBKgRegistry).GetField("database", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var database = field.GetValue(Registry)!;
            database.GetType().GetMethod("ExecuteScript", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(database, [sql]);
        }
        public void Dispose() { OptionalRegistry?.Dispose(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
