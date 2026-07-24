using System.Text.Json;
using System.Text.RegularExpressions;

namespace FullSpectrum.Knowledge.TestHost;

public static class SchemaSubsetValidator
{
    public static IReadOnlyList<string> Validate(string instanceJson, string schemaJson)
    {
        using var instance = JsonDocument.Parse(instanceJson);
        using var schema = JsonDocument.Parse(schemaJson);
        var errors = new List<string>();
        ValidateElement(instance.RootElement, schema.RootElement, "$", errors);
        return errors;
    }

    public static IReadOnlyList<string> AuditSchemaDocument(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        var errors = new List<string>();
        var root = document.RootElement;
        if (!root.TryGetProperty("$schema", out var dialect) ||
            dialect.GetString() != "https://json-schema.org/draft/2020-12/schema")
        {
            errors.Add("$schema must be JSON Schema Draft 2020-12.");
        }
        if (!root.TryGetProperty("$id", out _))
        {
            errors.Add("$id is required.");
        }
        return errors;
    }

    private static void ValidateElement(JsonElement instance, JsonElement schema, string path, List<string> errors)
    {
        if (schema.TryGetProperty("const", out var constant) && instance.GetRawText() != constant.GetRawText())
        {
            errors.Add($"{path}: value does not match const.");
        }

        if (schema.TryGetProperty("enum", out var values) &&
            !values.EnumerateArray().Any(x => x.GetRawText() == instance.GetRawText()))
        {
            errors.Add($"{path}: value is not in enum.");
        }

        if (schema.TryGetProperty("type", out var type))
        {
            if (type.ValueKind == JsonValueKind.Array)
            {
                var accepted = type.EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Any(expected => IsType(instance, expected));
                if (!accepted) errors.Add($"{path}: value does not match any allowed type.");
            }
            else
            {
                ValidateType(instance, type.GetString() ?? string.Empty, path, errors);
            }
        }

        if (instance.ValueKind == JsonValueKind.Object)
        {
            ValidateObject(instance, schema, path, errors);
        }
        else if (instance.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            var index = 0;
            foreach (var item in instance.EnumerateArray())
            {
                ValidateElement(item, items, $"{path}[{index++}]", errors);
            }
        }
        else if (instance.ValueKind == JsonValueKind.String)
        {
            ValidateString(instance.GetString() ?? string.Empty, schema, path, errors);
        }
        else if (instance.ValueKind == JsonValueKind.Number &&
                 schema.TryGetProperty("minimum", out var minimum) &&
                 instance.GetDecimal() < minimum.GetDecimal())
        {
            errors.Add($"{path}: number is below minimum.");
        }
    }

    private static void ValidateObject(JsonElement instance, JsonElement schema, string path, List<string> errors)
    {
        if (schema.TryGetProperty("required", out var required))
        {
            foreach (var name in required.EnumerateArray().Select(x => x.GetString()!))
            {
                if (!instance.TryGetProperty(name, out _))
                {
                    errors.Add($"{path}: missing required property '{name}'.");
                }
            }
        }

        var hasProperties = schema.TryGetProperty("properties", out var properties);
        foreach (var property in instance.EnumerateObject())
        {
            if (hasProperties && properties.TryGetProperty(property.Name, out var propertySchema))
            {
                ValidateElement(property.Value, propertySchema, $"{path}.{property.Name}", errors);
                continue;
            }

            if (schema.TryGetProperty("additionalProperties", out var additional))
            {
                if (additional.ValueKind == JsonValueKind.False)
                {
                    errors.Add($"{path}: additional property '{property.Name}' is forbidden.");
                }
                else if (additional.ValueKind == JsonValueKind.Object)
                {
                    ValidateElement(property.Value, additional, $"{path}.{property.Name}", errors);
                }
            }
        }
    }

    private static void ValidateString(string value, JsonElement schema, string path, List<string> errors)
    {
        if (schema.TryGetProperty("minLength", out var minimum) && value.Length < minimum.GetInt32())
        {
            errors.Add($"{path}: string is shorter than minLength.");
        }
        if (schema.TryGetProperty("pattern", out var pattern) &&
            !Regex.IsMatch(value, pattern.GetString()!, RegexOptions.CultureInvariant))
        {
            errors.Add($"{path}: string does not match pattern.");
        }
        if (schema.TryGetProperty("format", out var format) &&
            format.GetString() == "date-time" &&
            !DateTimeOffset.TryParse(value, out _))
        {
            errors.Add($"{path}: string is not a date-time.");
        }
    }

    private static void ValidateType(JsonElement instance, string expected, string path, List<string> errors)
    {
        var valid = IsType(instance, expected);
        if (!valid)
        {
            errors.Add($"{path}: expected type {expected}, got {instance.ValueKind}.");
        }
    }

    private static bool IsType(JsonElement instance, string expected) =>
        expected switch
        {
            "object" => instance.ValueKind == JsonValueKind.Object,
            "array" => instance.ValueKind == JsonValueKind.Array,
            "string" => instance.ValueKind == JsonValueKind.String,
            "integer" => instance.ValueKind == JsonValueKind.Number && instance.TryGetInt64(out _),
            "number" => instance.ValueKind == JsonValueKind.Number,
            "boolean" => instance.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => instance.ValueKind == JsonValueKind.Null,
            _ => true
        };
}
