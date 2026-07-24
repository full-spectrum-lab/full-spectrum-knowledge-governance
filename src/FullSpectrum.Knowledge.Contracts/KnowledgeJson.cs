using System.Text.Json;
using System.Text.Json.Serialization;

namespace FullSpectrum.Knowledge.Contracts;

public static class KnowledgeJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
        options.Converters.Add(new KnowledgeIdJsonConverter());
        options.Converters.Add(new KnowledgeVersionJsonConverter());
        return options;
    }

    private sealed class KnowledgeIdJsonConverter : JsonConverter<KnowledgeId>
    {
        public override KnowledgeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString() ?? throw new JsonException("KnowledgeId must be a string."));

        public override void Write(Utf8JsonWriter writer, KnowledgeId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }

    private sealed class KnowledgeVersionJsonConverter : JsonConverter<KnowledgeVersion>
    {
        public override KnowledgeVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString() ?? throw new JsonException("KnowledgeVersion must be a string."));

        public override void Write(Utf8JsonWriter writer, KnowledgeVersion value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }
}
