using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Domain;

namespace FullSpectrum.Knowledge.Storage;

/// <summary>
/// Offline, append-only persistence for K2 source registrations and retrieval
/// envelopes. This registry never performs network access or fixed-knowledge
/// promotion.
/// </summary>
public sealed class ControlledSourceRegistry : IDisposable
{
    private readonly SqliteDatabase database;

    public ControlledSourceRegistry(string databasePath)
    {
        database = new SqliteDatabase(databasePath);
        database.ExecuteScript("""
            CREATE TABLE IF NOT EXISTS kg_source_registration (
                source_id TEXT NOT NULL,
                source_version TEXT NOT NULL,
                payload TEXT NOT NULL,
                PRIMARY KEY (source_id, source_version)
            );
            CREATE TABLE IF NOT EXISTS kg_source_retrieval (
                retrieval_id TEXT PRIMARY KEY,
                request_identity TEXT NOT NULL,
                payload TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_kg_source_retrieval_request
                ON kg_source_retrieval(request_identity);
            CREATE TABLE IF NOT EXISTS kg_dynamic_snapshot (
                snapshot_id TEXT PRIMARY KEY,
                payload TEXT NOT NULL
            );
            """);
    }

    public KnowledgeSourceRegistration Register(KnowledgeSourceRegistration registration)
    {
        ControlledSourceValidator.ValidateRegistration(registration);
        var payload = Serialize(registration);
        var existing = database.Query(
            "SELECT payload FROM kg_source_registration WHERE source_id = ? AND source_version = ?",
            row => row.Text(0), registration.SourceId, registration.SourceVersion.Value).SingleOrDefault();
        if (existing is not null)
        {
            if (string.Equals(existing, payload, StringComparison.Ordinal)) return registration;
            throw new InvalidOperationException("Conflicting source registration already exists.");
        }

        database.Execute(
            "INSERT INTO kg_source_registration(source_id, source_version, payload) VALUES (?, ?, ?)",
            registration.SourceId, registration.SourceVersion.Value, payload);
        return registration;
    }

    public KnowledgeSourceRegistration? Get(string sourceId, KnowledgeVersion sourceVersion)
    {
        var payload = database.Query(
            "SELECT payload FROM kg_source_registration WHERE source_id = ? AND source_version = ?",
            row => row.Text(0), sourceId, sourceVersion.Value).SingleOrDefault();
        return payload is null ? null : JsonSerializer.Deserialize<KnowledgeSourceRegistration>(payload, KnowledgeJson.Options);
    }

    public KnowledgeSourceRetrieval RecordRetrieval(KnowledgeSourceRetrieval retrieval)
    {
        var registration = Get(retrieval.SourceId, retrieval.SourceVersion)
            ?? throw new InvalidOperationException("Source registration does not exist.");
        ControlledSourceValidator.ValidateRetrieval(registration, retrieval);
        var payload = Serialize(retrieval);
        var existing = database.Query(
            "SELECT payload FROM kg_source_retrieval WHERE retrieval_id = ?",
            row => row.Text(0), retrieval.RetrievalId).SingleOrDefault();
        if (existing is not null)
        {
            if (string.Equals(existing, payload, StringComparison.Ordinal)) return retrieval;
            throw new InvalidOperationException("Conflicting retrieval retry already exists.");
        }

        database.Execute(
            "INSERT INTO kg_source_retrieval(retrieval_id, request_identity, payload) VALUES (?, ?, ?)",
            retrieval.RetrievalId, retrieval.RequestIdentity, payload);
        return retrieval;
    }

    public KnowledgeSourceRetrieval? GetRetrieval(string retrievalId)
    {
        var payload = database.Query(
            "SELECT payload FROM kg_source_retrieval WHERE retrieval_id = ?",
            row => row.Text(0), retrievalId).SingleOrDefault();
        return payload is null ? null : JsonSerializer.Deserialize<KnowledgeSourceRetrieval>(payload, KnowledgeJson.Options);
    }

    public DynamicKnowledgeSnapshot SaveSnapshot(DynamicKnowledgeSnapshot snapshot)
    {
        var registration = Get(snapshot.SourceId, snapshot.SourceVersion)
            ?? throw new InvalidOperationException("Source registration does not exist.");
        ControlledSourceValidator.ValidateSnapshot(registration, snapshot);
        var payload = Serialize(snapshot);
        var existing = database.Query("SELECT payload FROM kg_dynamic_snapshot WHERE snapshot_id = ?", row => row.Text(0), snapshot.SnapshotId).SingleOrDefault();
        if (existing is not null)
        {
            if (string.Equals(existing, payload, StringComparison.Ordinal)) return snapshot;
            throw new InvalidOperationException("Conflicting snapshot overwrite is forbidden.");
        }
        database.Execute("INSERT INTO kg_dynamic_snapshot(snapshot_id, payload) VALUES (?, ?)", snapshot.SnapshotId, payload);
        return snapshot;
    }

    public DynamicKnowledgeSnapshot? GetSnapshot(string snapshotId)
    {
        var payload = database.Query("SELECT payload FROM kg_dynamic_snapshot WHERE snapshot_id = ?", row => row.Text(0), snapshotId).SingleOrDefault();
        return payload is null ? null : JsonSerializer.Deserialize<DynamicKnowledgeSnapshot>(payload, KnowledgeJson.Options);
    }

    private static string Serialize<T>(T value) =>
        DeterministicJson.Canonicalize(JsonSerializer.Serialize(value, KnowledgeJson.Options));

    public void Dispose() => database.Dispose();
}
