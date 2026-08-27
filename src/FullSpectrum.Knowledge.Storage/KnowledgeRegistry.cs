using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Storage;

public sealed class KnowledgeRegistry : IDisposable
{
    private readonly SqliteDatabase database;
    private readonly LocalArtifactStore artifacts;

    public KnowledgeRegistry(string databasePath, string artifactRoot)
    {
        database = new SqliteDatabase(databasePath);
        artifacts = new LocalArtifactStore(artifactRoot);
        database.ExecuteScript(
            """
            CREATE TABLE IF NOT EXISTS knowledge_packs (
              knowledge_id TEXT NOT NULL,
              version TEXT NOT NULL,
              state TEXT NOT NULL,
              pack_json TEXT NOT NULL,
              created_at_utc TEXT NOT NULL,
              PRIMARY KEY (knowledge_id, version)
            );
            CREATE TABLE IF NOT EXISTS knowledge_audit (
              sequence INTEGER PRIMARY KEY AUTOINCREMENT,
              knowledge_id TEXT NOT NULL,
              version TEXT NOT NULL,
              event_type TEXT NOT NULL,
              from_state TEXT NULL,
              to_state TEXT NOT NULL,
              actor TEXT NOT NULL,
              occurred_at_utc TEXT NOT NULL,
              details_json TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_knowledge_audit_identity
              ON knowledge_audit(knowledge_id, version, sequence);
            CREATE TABLE IF NOT EXISTS knowledge_resolutions (
              resolution_id TEXT PRIMARY KEY,
              request_id TEXT NOT NULL UNIQUE,
              request_json TEXT NOT NULL,
              result_json TEXT NOT NULL,
              result_digest TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS resolution_evidence (
              evidence_id TEXT PRIMARY KEY,
              resolution_id TEXT NOT NULL UNIQUE,
              evidence_json TEXT NOT NULL,
              evidence_digest TEXT NOT NULL
            );
            PRAGMA user_version=3;
            """);
    }

    public KnowledgePack Register(
        KnowledgePack pack,
        IReadOnlyList<ArtifactRegistration> content,
        string actor,
        DateTimeOffset occurredAtUtc)
    {
        RequireActor(actor);
        if (!KnowledgeContractVersions.IsSupported(pack.ContractVersion))
        {
            throw new ArgumentException("Unsupported contract version.", nameof(pack));
        }
        if (pack.State != KnowledgeLifecycleState.Draft)
        {
            throw new ArgumentException("New packs must be registered in DRAFT state.", nameof(pack));
        }
        if (content.Count != pack.Artifacts.Count)
        {
            throw new ArgumentException("Every declared artifact must have exactly one content registration.", nameof(content));
        }

        var byId = content.ToDictionary(item => item.ArtifactId, StringComparer.Ordinal);
        if (byId.Count != content.Count)
        {
            throw new ArgumentException("Artifact registrations must have unique IDs.", nameof(content));
        }
        if (pack.Artifacts.Select(item => item.ArtifactId).Distinct(StringComparer.Ordinal).Count() != pack.Artifacts.Count)
        {
            throw new ArgumentException("Declared artifacts must have unique IDs.", nameof(pack));
        }
        foreach (var artifact in pack.Artifacts)
        {
            if (!byId.TryGetValue(artifact.ArtifactId, out var registration))
            {
                throw new ArgumentException($"Missing content for '{artifact.ArtifactId}'.", nameof(content));
            }
            artifacts.Put(artifact, registration.Content.Span);
        }

        var packJson = DeterministicJson.Canonicalize(JsonSerializer.Serialize(pack, KnowledgeJson.Options));
        return database.Transaction(() =>
        {
            try
            {
                database.Execute(
                    "INSERT INTO knowledge_packs(knowledge_id,version,state,pack_json,created_at_utc) VALUES(?,?,?,?,?);",
                    pack.KnowledgeId.Value,
                    pack.Version.Value,
                    State(pack.State),
                    packJson,
                    occurredAtUtc.ToUniversalTime().ToString("O"));
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                throw new KnowledgeConflictException($"Pack '{pack.KnowledgeId}/{pack.Version}' already exists.");
            }
            AppendAudit(pack, "REGISTERED", null, pack.State, actor, occurredAtUtc);
            return pack;
        });
    }

    public KnowledgePack Get(KnowledgeId id, KnowledgeVersion version)
    {
        var rows = database.Query(
            "SELECT pack_json,state FROM knowledge_packs WHERE knowledge_id=? AND version=?;",
            row => (Json: row.Text(0), State: ParseState(row.Text(1))),
            id.Value,
            version.Value);
        if (rows.Count == 0) throw new KnowledgeNotFoundException($"Pack '{id}/{version}' was not found.");
        var pack = JsonSerializer.Deserialize<KnowledgePack>(rows[0].Json, KnowledgeJson.Options)
            ?? throw new InvalidDataException("Stored pack JSON is invalid.");
        return pack with { State = rows[0].State };
    }

    public KnowledgePack SubmitReview(KnowledgeId id, KnowledgeVersion version, string actor, DateTimeOffset at) =>
        Transition(id, version, KnowledgeLifecycleState.Draft, KnowledgeLifecycleState.ReviewRequired, "REVIEW_REQUESTED", actor, at);

    public KnowledgePack Release(KnowledgeId id, KnowledgeVersion version, string actor, DateTimeOffset at) =>
        Transition(id, version, KnowledgeLifecycleState.ReviewRequired, KnowledgeLifecycleState.Released, "RELEASED", actor, at);

    public KnowledgePack Revoke(KnowledgeId id, KnowledgeVersion version, string actor, DateTimeOffset at) =>
        Transition(id, version, KnowledgeLifecycleState.Released, KnowledgeLifecycleState.Revoked, "REVOKED", actor, at);

    public KnowledgePack Supersede(KnowledgeId id, KnowledgeVersion version, string actor, DateTimeOffset at) =>
        Transition(id, version, KnowledgeLifecycleState.Released, KnowledgeLifecycleState.Superseded, "SUPERSEDED", actor, at);

    public KnowledgePack UpgradeContract(
        KnowledgeId id,
        KnowledgeVersion version,
        string targetContractVersion,
        string actor,
        DateTimeOffset at)
    {
        RequireActor(actor);
        if (!string.Equals(targetContractVersion, KnowledgeContractVersions.V1_1, StringComparison.Ordinal))
        {
            throw new ArgumentException("Only the explicit v1.0 to v1.1 upgrade is supported.", nameof(targetContractVersion));
        }

        return database.Transaction(() =>
        {
            var current = Get(id, version);
            if (string.Equals(current.ContractVersion, targetContractVersion, StringComparison.Ordinal))
            {
                if (AuditContains(id, version, "CONTRACT_UPGRADED", new Dictionary<string, string>
                {
                    ["from_contract_version"] = KnowledgeContractVersions.V1_0,
                    ["to_contract_version"] = KnowledgeContractVersions.V1_1
                }))
                {
                    return current;
                }
                throw new KnowledgeConflictException("Contract is already v1.1 without a matching upgrade record.");
            }
            if (!string.Equals(current.ContractVersion, KnowledgeContractVersions.V1_0, StringComparison.Ordinal))
            {
                throw new KnowledgeConflictException(
                    $"Contract '{current.ContractVersion}' cannot be upgraded to '{targetContractVersion}'.");
            }

            var updated = current with { ContractVersion = targetContractVersion };
            var json = DeterministicJson.Canonicalize(JsonSerializer.Serialize(updated, KnowledgeJson.Options));
            var changed = database.Execute(
                "UPDATE knowledge_packs SET pack_json=? WHERE knowledge_id=? AND version=? AND pack_json=?;",
                json,
                id.Value,
                version.Value,
                DeterministicJson.Canonicalize(JsonSerializer.Serialize(current, KnowledgeJson.Options)));
            if (changed != 1) throw new KnowledgeConflictException("Concurrent contract upgrade detected.");
            AppendAudit(updated, "CONTRACT_UPGRADED", current.State, current.State, actor, at,
                new Dictionary<string, string>
                {
                    ["from_contract_version"] = KnowledgeContractVersions.V1_0,
                    ["to_contract_version"] = KnowledgeContractVersions.V1_1
                });
            return updated;
        });
    }

    public KnowledgePack Supersede(
        KnowledgeId id,
        KnowledgeVersion version,
        KnowledgeReference replacement,
        string actor,
        DateTimeOffset at)
    {
        RequireActor(actor);
        if (replacement.KnowledgeId != id || replacement.Version == version)
        {
            throw new ArgumentException(
                "A superseding reference must name a different version of the same Knowledge ID.",
                nameof(replacement));
        }
        var details = new Dictionary<string, string>
        {
            ["replacement_knowledge_id"] = replacement.KnowledgeId.Value,
            ["replacement_version"] = replacement.Version.Value
        };

        return database.Transaction(() =>
        {
            var current = Get(id, version);
            if (current.State == KnowledgeLifecycleState.Superseded)
            {
                if (AuditContains(id, version, "SUPERSEDED", details)) return current;
                throw new KnowledgeConflictException("Pack was superseded by a different exact reference.");
            }
            if (current.State != KnowledgeLifecycleState.Released)
            {
                throw new KnowledgeConflictException(
                    $"Transition {current.State} -> Superseded is not allowed for '{id}/{version}'.");
            }
            var replacementPack = Get(replacement.KnowledgeId, replacement.Version);
            if (replacementPack.State != KnowledgeLifecycleState.Released)
            {
                throw new KnowledgeConflictException("The exact superseding pack must be RELEASED.");
            }
            return TransitionWithinTransaction(
                current,
                KnowledgeLifecycleState.Released,
                KnowledgeLifecycleState.Superseded,
                "SUPERSEDED",
                actor,
                at,
                details);
        });
    }

    public KnowledgePack Tombstone(
        KnowledgeId id,
        KnowledgeVersion version,
        string reason,
        string actor,
        DateTimeOffset at)
    {
        RequireActor(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var details = new Dictionary<string, string> { ["reason"] = reason };

        return database.Transaction(() =>
        {
            var current = Get(id, version);
            if (current.State == KnowledgeLifecycleState.Tombstoned)
            {
                if (AuditContains(id, version, "TOMBSTONED", details)) return current;
                throw new KnowledgeConflictException("Pack was tombstoned with different details.");
            }
            if (!string.Equals(current.ContractVersion, KnowledgeContractVersions.V1_1, StringComparison.Ordinal))
            {
                throw new KnowledgeConflictException("Tombstone requires an explicit upgrade to knowledge-contract/1.1.0.");
            }
            return TransitionWithinTransaction(
                current,
                current.State,
                KnowledgeLifecycleState.Tombstoned,
                "TOMBSTONED",
                actor,
                at,
                details);
        });
    }

    public IReadOnlyList<KnowledgeAuditEvent> Audit(KnowledgeId id, KnowledgeVersion version) =>
        database.Query(
            """
            SELECT sequence,event_type,from_state,to_state,actor,occurred_at_utc,details_json
            FROM knowledge_audit WHERE knowledge_id=? AND version=? ORDER BY sequence;
            """,
            row => new KnowledgeAuditEvent(
                row.Int64(0),
                id,
                version,
                row.Text(1),
                row.NullableText(2) is { } from ? ParseState(from) : null,
                ParseState(row.Text(3)),
                row.Text(4),
                DateTimeOffset.Parse(row.Text(5), System.Globalization.CultureInfo.InvariantCulture),
                JsonSerializer.Deserialize<Dictionary<string, string>>(row.Text(6), KnowledgeJson.Options)
                    ?? new Dictionary<string, string>()),
            id.Value,
            version.Value);

    public KnowledgeReplay Replay(KnowledgeId id, KnowledgeVersion version, long? throughSequence = null)
    {
        var events = Audit(id, version)
            .Where(item => throughSequence is null || item.Sequence <= throughSequence.Value)
            .ToArray();
        if (events.Length == 0) throw new KnowledgeNotFoundException($"No audit history for '{id}/{version}'.");
        return new KnowledgeReplay(id, version, events[^1].Sequence, events[^1].ToState, events);
    }

    public KnowledgeReplay ReplayExact(KnowledgeId id, KnowledgeVersion version, long sequence)
    {
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        var events = Audit(id, version).Where(item => item.Sequence <= sequence).ToArray();
        if (events.Length == 0 || events[^1].Sequence != sequence)
        {
            throw new KnowledgeNotFoundException(
                $"Audit sequence '{sequence}' does not belong to '{id}/{version}'.");
        }
        return new KnowledgeReplay(id, version, sequence, events[^1].ToState, events);
    }

    public byte[] ReadArtifact(KnowledgeId id, KnowledgeVersion version, string artifactId)
    {
        var pack = Get(id, version);
        var artifact = pack.Artifacts.SingleOrDefault(
            item => string.Equals(item.ArtifactId, artifactId, StringComparison.Ordinal))
            ?? throw new KnowledgeNotFoundException($"Artifact '{artifactId}' was not found.");
        return artifacts.Read(artifact.Digest);
    }

    public KnowledgeResolutionResult SaveResolution(
        KnowledgeResolutionRequest request,
        KnowledgeResolutionResult result)
    {
        var requestJson = DeterministicJson.Canonicalize(
            JsonSerializer.Serialize(request, KnowledgeJson.Options));
        var resultJson = DeterministicJson.Canonicalize(
            JsonSerializer.Serialize(result, KnowledgeJson.Options));
        return database.Transaction(() =>
        {
            var rows = database.Query(
                "SELECT request_json,result_json FROM knowledge_resolutions WHERE request_id=?;",
                row => (Request: row.Text(0), Result: row.Text(1)),
                request.RequestId);
            if (rows.Count != 0)
            {
                if (!string.Equals(rows[0].Request, requestJson, StringComparison.Ordinal) ||
                    !string.Equals(rows[0].Result, resultJson, StringComparison.Ordinal))
                {
                    throw new KnowledgeConflictException(
                        $"Request '{request.RequestId}' already has a different resolution.");
                }
                return JsonSerializer.Deserialize<KnowledgeResolutionResult>(
                    rows[0].Result, KnowledgeJson.Options)
                    ?? throw new InvalidDataException("Stored resolution JSON is invalid.");
            }

            database.Execute(
                """
                INSERT INTO knowledge_resolutions
                  (resolution_id,request_id,request_json,result_json,result_digest)
                VALUES(?,?,?,?,?);
                """,
                result.ResolutionId,
                request.RequestId,
                requestJson,
                resultJson,
                result.ResultDigest.Value);
            return result;
        });
    }

    public KnowledgeResolutionResult? TryGetResolutionByRequest(string requestId)
    {
        var rows = database.Query(
            "SELECT result_json FROM knowledge_resolutions WHERE request_id=?;",
            row => row.Text(0),
            requestId);
        return rows.Count == 0
            ? null
            : JsonSerializer.Deserialize<KnowledgeResolutionResult>(rows[0], KnowledgeJson.Options)
              ?? throw new InvalidDataException("Stored resolution JSON is invalid.");
    }

    public KnowledgeResolutionResult GetResolution(string resolutionId)
    {
        var rows = database.Query(
            "SELECT result_json FROM knowledge_resolutions WHERE resolution_id=?;",
            row => row.Text(0),
            resolutionId);
        if (rows.Count == 0)
        {
            throw new KnowledgeNotFoundException($"Resolution '{resolutionId}' was not found.");
        }
        return JsonSerializer.Deserialize<KnowledgeResolutionResult>(rows[0], KnowledgeJson.Options)
            ?? throw new InvalidDataException("Stored resolution JSON is invalid.");
    }

    public KnowledgeResolutionEvidence SaveEvidence(KnowledgeResolutionEvidence evidence)
    {
        _ = GetResolution(evidence.ResolutionId);
        var json = DeterministicJson.Canonicalize(
            JsonSerializer.Serialize(evidence, KnowledgeJson.Options));
        return database.Transaction(() =>
        {
            var rows = database.Query(
                "SELECT evidence_json FROM resolution_evidence WHERE resolution_id=?;",
                row => row.Text(0),
                evidence.ResolutionId);
            if (rows.Count != 0)
            {
                if (!string.Equals(rows[0], json, StringComparison.Ordinal))
                {
                    throw new KnowledgeConflictException(
                        $"Resolution '{evidence.ResolutionId}' already has different evidence.");
                }
                return JsonSerializer.Deserialize<KnowledgeResolutionEvidence>(
                    rows[0], KnowledgeJson.Options)
                    ?? throw new InvalidDataException("Stored evidence JSON is invalid.");
            }
            database.Execute(
                """
                INSERT INTO resolution_evidence
                  (evidence_id,resolution_id,evidence_json,evidence_digest)
                VALUES(?,?,?,?);
                """,
                evidence.EvidenceId,
                evidence.ResolutionId,
                json,
                evidence.EvidenceDigest.Value);
            return evidence;
        });
    }

    public KnowledgeResolutionEvidence GetEvidence(string evidenceId)
    {
        var rows = database.Query(
            "SELECT evidence_json FROM resolution_evidence WHERE evidence_id=?;",
            row => row.Text(0),
            evidenceId);
        if (rows.Count == 0) throw new KnowledgeNotFoundException($"Evidence '{evidenceId}' was not found.");
        return JsonSerializer.Deserialize<KnowledgeResolutionEvidence>(rows[0], KnowledgeJson.Options)
            ?? throw new InvalidDataException("Stored evidence JSON is invalid.");
    }

    private KnowledgePack Transition(
        KnowledgeId id,
        KnowledgeVersion version,
        KnowledgeLifecycleState expected,
        KnowledgeLifecycleState next,
        string eventType,
        string actor,
        DateTimeOffset at)
    {
        RequireActor(actor);
        return database.Transaction(() =>
        {
            var current = Get(id, version);
            if (current.State != expected)
            {
                throw new KnowledgeConflictException(
                    $"Transition {current.State} -> {next} is not allowed for '{id}/{version}'.");
            }
            return TransitionWithinTransaction(current, expected, next, eventType, actor, at);
        });
    }

    private KnowledgePack TransitionWithinTransaction(
        KnowledgePack current,
        KnowledgeLifecycleState expected,
        KnowledgeLifecycleState next,
        string eventType,
        string actor,
        DateTimeOffset at,
        IReadOnlyDictionary<string, string>? details = null)
    {
        var updated = current with { State = next };
        var json = DeterministicJson.Canonicalize(JsonSerializer.Serialize(updated, KnowledgeJson.Options));
        var changed = database.Execute(
            "UPDATE knowledge_packs SET state=?,pack_json=? WHERE knowledge_id=? AND version=? AND state=?;",
            State(next),
            json,
            current.KnowledgeId.Value,
            current.Version.Value,
            State(expected));
        if (changed != 1) throw new KnowledgeConflictException("Concurrent lifecycle update detected.");
        AppendAudit(updated, eventType, expected, next, actor, at, details);
        return updated;
    }

    private void AppendAudit(
        KnowledgePack pack,
        string eventType,
        KnowledgeLifecycleState? from,
        KnowledgeLifecycleState to,
        string actor,
        DateTimeOffset at,
        IReadOnlyDictionary<string, string>? details = null)
    {
        database.Insert(
            """
            INSERT INTO knowledge_audit
              (knowledge_id,version,event_type,from_state,to_state,actor,occurred_at_utc,details_json)
            VALUES(?,?,?,?,?,?,?,?);
            """,
            pack.KnowledgeId.Value,
            pack.Version.Value,
            eventType,
            from is null ? null : State(from.Value),
            State(to),
            actor,
            at.ToUniversalTime().ToString("O"),
            DeterministicJson.Canonicalize(JsonSerializer.Serialize(
                details ?? new Dictionary<string, string>(), KnowledgeJson.Options)));
    }

    private bool AuditContains(
        KnowledgeId id,
        KnowledgeVersion version,
        string eventType,
        IReadOnlyDictionary<string, string> details)
    {
        var rows = database.Query(
            """
            SELECT event_type,details_json FROM knowledge_audit
            WHERE knowledge_id=? AND version=? AND event_type=? ORDER BY sequence;
            """,
            row => (EventType: row.Text(0), Details: row.Text(1)),
            id.Value,
            version.Value,
            eventType);
        var expected = DeterministicJson.Canonicalize(JsonSerializer.Serialize(details, KnowledgeJson.Options));
        return rows.Any(row =>
            string.Equals(row.EventType, eventType, StringComparison.Ordinal) &&
            string.Equals(row.Details, expected, StringComparison.Ordinal));
    }

    private static string State(KnowledgeLifecycleState state) =>
        JsonNamingPolicy.SnakeCaseUpper.ConvertName(state.ToString());

    private static KnowledgeLifecycleState ParseState(string value) =>
        Enum.Parse<KnowledgeLifecycleState>(
            string.Concat(value.Split('_').Select(
                part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant())),
            ignoreCase: false);

    private static void RequireActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
    }

    public void Dispose() => database.Dispose();
}
