using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Contracts.FdeStageB;
using FullSpectrum.Knowledge.Storage.FdeStageB;

namespace FullSpectrum.Knowledge.StageBPort;

public sealed class FdeStageBPortSession : IDisposable
{
    public const string ProtocolVersion = "FDE-STAGE-B-I2-PORT-V1";
    private static readonly JsonSerializerOptions JsonOptions = new(KnowledgeJson.Options) { WriteIndented = false };
    private readonly string databasePath;
    private readonly HashSet<string> requestIds = new(StringComparer.Ordinal);
    private FdeStageBKgRegistry registry;

    public FdeStageBPortSession(string databasePath)
    {
        this.databasePath = Path.GetFullPath(databasePath);
        registry = new FdeStageBKgRegistry(this.databasePath);
    }

    public string ProcessLine(string line)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        RequireExactProperties(root, "protocol_version", "request_id", "operation", "payload");
        var version = RequiredString(root, "protocol_version");
        var requestId = RequiredString(root, "request_id");
        if (!string.Equals(version, ProtocolVersion, StringComparison.Ordinal))
            return Error(requestId, "PROTOCOL_VERSION_UNSUPPORTED", "Unsupported Stage B port protocol version.");
        if (!requestIds.Add(requestId))
            return Error(requestId, "REQUEST_ID_REUSED", "request_id must be unique for the lifetime of the port process.");

        try
        {
            var operation = RequiredString(root, "operation");
            var payload = root.GetProperty("payload");
            object? result = operation switch
            {
                "append" => Append(payload),
                "get" => Get(payload),
                "replay" => Replay(payload),
                "revision_root" => RevisionRoot(payload),
                "close_reopen" => CloseReopen(payload),
                _ => throw new PortRequestException("OPERATION_UNKNOWN", "Unknown Stage B port operation.")
            };
            return JsonSerializer.Serialize(new
            {
                protocol_version = ProtocolVersion,
                request_id = requestId,
                ok = true,
                result
            }, JsonOptions);
        }
        catch (Exception exception) when (exception is PortRequestException or FdeKgPersistenceException or JsonException or InvalidOperationException)
        {
            var code = exception switch
            {
                PortRequestException port => port.Code,
                FdeKgPersistenceException persistence => persistence.ErrorCode,
                JsonException => "REQUEST_JSON_INVALID",
                _ => "PORT_OPERATION_FAILED"
            };
            return Error(requestId, code, exception.Message);
        }
    }

    private object Append(JsonElement payload)
    {
        RequireExactProperties(payload, "event");
        var value = DeserializeEvent(payload.GetProperty("event"));
        FdeKgEvent appended = value switch
        {
            FdeEnginePreActionAudit item => registry.Append(item),
            FdeHumanDecision item => registry.Append(item),
            FdeActionIntent item => registry.Append(item),
            FdeActionReceiptDirect item => registry.Append(item),
            FdeActionReceiptReconciled item => registry.Append(item),
            FdeKgFailureCopy item => registry.Append(item),
            FdeKgNoActionCopy item => registry.Append(item),
            _ => throw new PortRequestException("EVENT_TYPE_FORBIDDEN", "KG does not accept this event type.")
        };
        return new { event_global_ref = $"kg:{appended.EventId}", payload_sha256 = appended.PayloadSha256 };
    }

    private object Get(JsonElement payload)
    {
        RequireExactProperties(payload, "event_global_ref");
        var value = registry.Get(RequiredString(payload, "event_global_ref"));
        return value is null
            ? new { status = "NOT_FOUND", @event = (object?)null }
            : new { status = "FOUND", @event = (object?)value };
    }

    private object Replay(JsonElement payload)
    {
        RequireExactProperties(payload, "run_id");
        var runId = RequiredUuidV7(payload, "run_id");
        return new { events = registry.Replay(runId) };
    }

    private object RevisionRoot(JsonElement payload)
    {
        RequireExactProperties(payload);
        return registry.RevisionRoot();
    }

    private object CloseReopen(JsonElement payload)
    {
        RequireExactProperties(payload);
        registry.Dispose();
        registry = new FdeStageBKgRegistry(databasePath);
        return new { status = "REOPENED" };
    }

    private static FdeKgEvent DeserializeEvent(JsonElement element)
    {
        if (!element.TryGetProperty("event_type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            throw new PortRequestException("EVENT_TYPE_REQUIRED", "event_type is required.");
        var json = element.GetRawText();
        return typeElement.GetString() switch
        {
            FdeStageBEventTypes.EnginePreActionAudit => JsonSerializer.Deserialize<FdeEnginePreActionAudit>(json, KnowledgeJson.Options)!,
            FdeStageBEventTypes.HumanDecision => JsonSerializer.Deserialize<FdeHumanDecision>(json, KnowledgeJson.Options)!,
            FdeStageBEventTypes.ActionIntent => JsonSerializer.Deserialize<FdeActionIntent>(json, KnowledgeJson.Options)!,
            FdeStageBEventTypes.ActionReceiptDirect => JsonSerializer.Deserialize<FdeActionReceiptDirect>(json, KnowledgeJson.Options)!,
            FdeStageBEventTypes.ActionReceiptReconciled => JsonSerializer.Deserialize<FdeActionReceiptReconciled>(json, KnowledgeJson.Options)!,
            FdeStageBEventTypes.KgFailureCopy => JsonSerializer.Deserialize<FdeKgFailureCopy>(json, KnowledgeJson.Options)!,
            FdeStageBEventTypes.KgNoActionCopy => JsonSerializer.Deserialize<FdeKgNoActionCopy>(json, KnowledgeJson.Options)!,
            _ => throw new PortRequestException("EVENT_TYPE_FORBIDDEN", "KG does not accept this event type.")
        };
    }

    private static void RequireExactProperties(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new PortRequestException("REQUEST_SHAPE_INVALID", "JSON value must be an object.");
        var actual = value.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        var required = expected.Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(required, StringComparer.Ordinal))
            throw new PortRequestException("REQUEST_SHAPE_INVALID", "JSON object properties are not complete and exact.");
    }

    private static string RequiredString(JsonElement value, string property)
    {
        if (!value.TryGetProperty(property, out var element) || element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
            throw new PortRequestException("REQUEST_FIELD_INVALID", $"{property} must be a non-empty string.");
        return element.GetString()!;
    }

    private static Guid RequiredUuidV7(JsonElement value, string property)
    {
        var text = RequiredString(value, property);
        if (!Guid.TryParse(text, out var result) || result.Version != 7)
            throw new PortRequestException("UUIDV7_REQUIRED", $"{property} must be UUIDv7.");
        return result;
    }

    private static string Error(string requestId, string code, string message) => JsonSerializer.Serialize(new
    {
        protocol_version = ProtocolVersion,
        request_id = requestId,
        ok = false,
        error = new { code, message }
    }, JsonOptions);

    public void Dispose() => registry.Dispose();

    private sealed class PortRequestException(string code, string message) : InvalidOperationException(message)
    {
        internal string Code { get; } = code;
    }
}
