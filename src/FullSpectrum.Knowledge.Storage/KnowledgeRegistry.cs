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
            PRAGMA user_version=1;
            """);
    }

    public KnowledgePack Register(
        KnowledgePack pack,
        IReadOnlyList<ArtifactRegistration> content,
        string actor,
        DateTimeOffset occurredAtUtc)
    {
        RequireActor(actor);
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

    public byte[] ReadArtifact(KnowledgeId id, KnowledgeVersion version, string artifactId)
    {
        var pack = Get(id, version);
        var artifact = pack.Artifacts.SingleOrDefault(
            item => string.Equals(item.ArtifactId, artifactId, StringComparison.Ordinal))
            ?? throw new KnowledgeNotFoundException($"Artifact '{artifactId}' was not found.");
        return artifacts.Read(artifact.Digest);
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
            var updated = current with { State = next };
            var json = DeterministicJson.Canonicalize(JsonSerializer.Serialize(updated, KnowledgeJson.Options));
            var changed = database.Execute(
                "UPDATE knowledge_packs SET state=?,pack_json=? WHERE knowledge_id=? AND version=? AND state=?;",
                State(next),
                json,
                id.Value,
                version.Value,
                State(expected));
            if (changed != 1) throw new KnowledgeConflictException("Concurrent lifecycle update detected.");
            AppendAudit(updated, eventType, expected, next, actor, at);
            return updated;
        });
    }

    private void AppendAudit(
        KnowledgePack pack,
        string eventType,
        KnowledgeLifecycleState? from,
        KnowledgeLifecycleState to,
        string actor,
        DateTimeOffset at)
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
            "{}");
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
