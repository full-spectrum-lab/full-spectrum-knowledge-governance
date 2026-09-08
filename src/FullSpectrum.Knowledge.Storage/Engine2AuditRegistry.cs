using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Storage;

public sealed record Engine2SnapshotBinding(
    string EngineCommit,
    string ObserverCommit,
    string KgCommit,
    string ProtocolSchemaVersion,
    string KnowledgeSnapshotRef);

public sealed record Engine2RetrievalResult(
    string RequestId,
    string Generation,
    string Decision,
    string VerificationStatus,
    string ResultDigest,
    string HardGate,
    string OriginalEngineResultDigest);

public sealed record Engine2AuditEvent(
    string EventId,
    string RequestId,
    string EventType,
    string Outcome,
    string OriginalEventRef,
    string InputDigest,
    string OutputDigest);

public sealed record Engine2AuditEnvelope(
    string IdempotencyKey,
    string ProtocolObjectJson,
    string ProtocolObjectDigest,
    Engine2SnapshotBinding SnapshotBinding,
    Engine2RetrievalResult EngineResult,
    Engine2AuditEvent AuditEvent,
    string AuditEventDigest,
    string ErrorCode);

public sealed class Engine2AuditRejectedException(
    string errorCode,
    string message,
    string retryPreconditions = "NONE",
    Exception? innerException = null) : InvalidOperationException(message, innerException)
{
    public string ErrorCode { get; } = errorCode;
    public bool Retryable => false;
    public string RetryPreconditions { get; } = retryPreconditions;
}

/// <summary>
/// Offline-only persistence boundary for frozen Engine 2 audit messages.
/// It verifies and stores Engine facts but never interprets or changes a decision.
/// </summary>
public sealed class Engine2AuditRegistry : IDisposable
{
    private static readonly HashSet<string> RegisteredErrorCodes = new(StringComparer.Ordinal)
    {
        "NONE",
        "PROTOCOL_OBJECT_INVALID",
        "PROTOCOL_OBJECT_UNTRUSTED",
        "CREDENTIAL_EXPIRED",
        "CREDENTIAL_REVOKED",
        "EVIDENCE_DIGEST_MISMATCH",
        "REPLAY_DEPENDENCY_MISSING",
        "IDEMPOTENCY_CONFLICT",
        "OBSERVER_RESULT_MUTATION_DETECTED",
        "AUDIT_PERSISTENCE_FAILED"
    };

    private readonly SqliteDatabase database;

    public Engine2AuditRegistry(string databasePath)
    {
        database = new SqliteDatabase(databasePath);
        database.ExecuteScript("""
            CREATE TABLE IF NOT EXISTS kg_engine2_audit (
                event_id TEXT PRIMARY KEY,
                idempotency_key TEXT NOT NULL UNIQUE,
                envelope_payload TEXT NOT NULL,
                envelope_digest TEXT NOT NULL
            );
            """);
    }

    public Engine2AuditEnvelope Append(Engine2AuditEnvelope envelope)
    {
        Validate(envelope);
        var payload = Engine2CanonicalJson.Serialize(envelope);
        var payloadDigest = Engine2CanonicalJson.ComputeSha256(payload);

        try
        {
            var existing = database.Query(
                "SELECT envelope_payload FROM kg_engine2_audit WHERE event_id = ? OR idempotency_key = ?",
                row => row.Text(0), envelope.AuditEvent.EventId, envelope.IdempotencyKey);
            if (existing.Count > 0)
            {
                if (existing.All(value => string.Equals(value, payload, StringComparison.Ordinal))) return envelope;
                throw Reject("IDEMPOTENCY_CONFLICT", "Engine 2 audit identity was reused with different content.");
            }

            return database.Transaction(() =>
            {
                database.Execute(
                    "INSERT INTO kg_engine2_audit(event_id, idempotency_key, envelope_payload, envelope_digest) VALUES (?, ?, ?, ?)",
                    envelope.AuditEvent.EventId, envelope.IdempotencyKey, payload, payloadDigest);
                return envelope;
            });
        }
        catch (Engine2AuditRejectedException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw Reject(
                "AUDIT_PERSISTENCE_FAILED",
                "Required Engine 2 audit persistence failed.",
                "RETRY_ONLY_WHEN_NO_EXTERNAL_SIDE_EFFECT_AND_IDEMPOTENCY_IS_PROVEN",
                exception);
        }
    }

    public Engine2AuditEnvelope Replay(string eventId)
    {
        try
        {
            var stored = database.Query(
                "SELECT envelope_payload, envelope_digest FROM kg_engine2_audit WHERE event_id = ?",
                row => (Payload: row.Text(0), Digest: row.Text(1)), eventId).SingleOrDefault();
            if (stored == default)
                throw Reject("REPLAY_DEPENDENCY_MISSING", "Engine 2 audit event was not found.");
            if (!string.Equals(Engine2CanonicalJson.ComputeSha256(stored.Payload), stored.Digest, StringComparison.Ordinal))
                throw Reject("EVIDENCE_DIGEST_MISMATCH", "Persisted Engine 2 audit envelope digest does not match.");

            Engine2AuditEnvelope envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<Engine2AuditEnvelope>(stored.Payload, KnowledgeJson.Options)
                    ?? throw new JsonException("Engine 2 audit envelope is null.");
            }
            catch (JsonException exception)
            {
                throw Reject("EVIDENCE_DIGEST_MISMATCH", "Persisted Engine 2 audit envelope is invalid.", innerException: exception);
            }
            Validate(envelope);
            return envelope;
        }
        catch (Engine2AuditRejectedException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw Reject("AUDIT_PERSISTENCE_FAILED", "Required Engine 2 audit replay failed.", innerException: exception);
        }
    }

    public static string ComputeRequestDigest(Engine2AuditEnvelope envelope) => Engine2CanonicalJson.ComputeSha256(new
    {
        generation = envelope.EngineResult.Generation,
        protocol_object = Engine2CanonicalJson.ParseSafeSubset(envelope.ProtocolObjectJson),
        protocol_object_digest = envelope.ProtocolObjectDigest,
        snapshot_binding = envelope.SnapshotBinding
    });

    public static string ComputeResultDigest(Engine2RetrievalResult result) => Engine2CanonicalJson.ComputeSha256(new
    {
        request_id = result.RequestId,
        generation = result.Generation,
        decision = result.Decision,
        verification_status = result.VerificationStatus,
        hard_gate = result.HardGate
    });

    private static void Validate(Engine2AuditEnvelope envelope)
    {
        Require(envelope.IdempotencyKey, nameof(envelope.IdempotencyKey));
        Require(envelope.AuditEvent.EventId, nameof(envelope.AuditEvent.EventId));
        Require(envelope.AuditEvent.RequestId, nameof(envelope.AuditEvent.RequestId));
        Require(envelope.EngineResult.RequestId, nameof(envelope.EngineResult.RequestId));
        Require(envelope.SnapshotBinding.EngineCommit, nameof(envelope.SnapshotBinding.EngineCommit));
        Require(envelope.SnapshotBinding.ObserverCommit, nameof(envelope.SnapshotBinding.ObserverCommit));
        Require(envelope.SnapshotBinding.KgCommit, nameof(envelope.SnapshotBinding.KgCommit));
        Require(envelope.SnapshotBinding.ProtocolSchemaVersion, nameof(envelope.SnapshotBinding.ProtocolSchemaVersion));
        Require(envelope.SnapshotBinding.KnowledgeSnapshotRef, nameof(envelope.SnapshotBinding.KnowledgeSnapshotRef));

        if (!RegisteredErrorCodes.Contains(envelope.ErrorCode))
            throw Reject("PROTOCOL_OBJECT_UNTRUSTED", "Engine 2 audit envelope contains an unregistered error code.");

        string protocolDigest;
        try
        {
            protocolDigest = Engine2CanonicalJson.ComputeSha256(envelope.ProtocolObjectJson);
        }
        catch (JsonException exception)
        {
            throw Reject("PROTOCOL_OBJECT_UNTRUSTED", "Protocol object is not valid canonical JSON input.", innerException: exception);
        }
        if (!Matches(protocolDigest, envelope.ProtocolObjectDigest))
            throw Reject("EVIDENCE_DIGEST_MISMATCH", "Protocol object digest does not match.");

        var resultDigest = ComputeResultDigest(envelope.EngineResult);
        if (!Matches(resultDigest, envelope.EngineResult.ResultDigest) ||
            !Matches(resultDigest, envelope.EngineResult.OriginalEngineResultDigest))
            throw Reject("EVIDENCE_DIGEST_MISMATCH", "Engine result digest does not match its immutable semantic fields.");

        if (!string.Equals(envelope.AuditEvent.RequestId, envelope.EngineResult.RequestId, StringComparison.Ordinal) ||
            !string.Equals(envelope.AuditEvent.Outcome, envelope.EngineResult.Decision, StringComparison.Ordinal) ||
            !Matches(envelope.AuditEvent.InputDigest, ComputeRequestDigest(envelope)) ||
            !Matches(envelope.AuditEvent.OutputDigest, resultDigest))
            throw Reject("EVIDENCE_DIGEST_MISMATCH", "Audit event is not bound to the supplied Engine request and result.");

        var eventDigest = Engine2CanonicalJson.ComputeSha256(envelope.AuditEvent);
        if (!Matches(eventDigest, envelope.AuditEventDigest))
            throw Reject("EVIDENCE_DIGEST_MISMATCH", "Audit event digest does not match.");
    }

    private static bool Matches(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static void Require(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Reject("PROTOCOL_OBJECT_UNTRUSTED", $"Required Engine 2 field '{field}' is empty.");
    }

    private static Engine2AuditRejectedException Reject(
        string code,
        string message,
        string retryPreconditions = "NONE",
        Exception? innerException = null) => new(code, message, retryPreconditions, innerException);

    public void Dispose() => database.Dispose();
}

public static class Engine2CanonicalJson
{
    public static string Serialize<T>(T value) =>
        Canonicalize(JsonSerializer.Serialize(value, KnowledgeJson.Options));

    public static string Canonicalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }))
        {
            WriteSafeSubset(writer, document.RootElement);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static JsonElement ParseSafeSubset(string json)
    {
        using var document = JsonDocument.Parse(json);
        _ = Canonicalize(json);
        return document.RootElement.Clone();
    }

    public static string ComputeSha256(string json) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonicalize(json))));

    public static string ComputeSha256<T>(T value) => ComputeSha256(Serialize(value));

    private static void WriteSafeSubset(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var properties = element.EnumerateObject().ToArray();
                if (properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
                    throw new JsonException("Duplicate JSON object keys are forbidden.");
                foreach (var property in properties.OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteSafeSubset(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteSafeSubset(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (!element.TryGetInt64(out var integer))
                    throw new JsonException("Only integers are allowed in the frozen Engine 2 safe subset.");
                writer.WriteNumberValue(integer);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new JsonException("Unsupported JSON value in the frozen Engine 2 safe subset.");
        }
    }
}
