using System.Text.Json;

namespace LogistiFlow.McpServer.UnitTests.Fixtures;

/// <summary>
/// The part of JSON Schema a client checks when it validates structured content: required properties, declared types
/// (including whether null is allowed), nested objects, array items and local <c>$ref</c>s. Returns every violation.
/// </summary>
public static class OutputSchema
{
    public static IReadOnlyList<string> Violations(JsonElement schema, JsonElement instance)
    {
        var violations = new List<string>();
        Check(schema, schema, instance, "$", violations);
        return violations;
    }

    private static void Check(JsonElement root, JsonElement schema, JsonElement instance, string path, List<string> violations)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (schema.TryGetProperty("$ref", out var reference))
        {
            Check(root, Resolve(root, reference.GetString()!), instance, path, violations);
            return;
        }

        if (schema.TryGetProperty("type", out var type) && !Allows(type, instance))
        {
            violations.Add($"{path}: {instance.ValueKind} is not allowed by type {type.GetRawText()}");
            return;
        }

        if (instance.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("required", out var required))
            {
                violations.AddRange(required.EnumerateArray()
                    .Select(name => name.GetString()!)
                    .Where(name => !instance.TryGetProperty(name, out _))
                    .Select(name => $"{path}: required property '{name}' is missing"));
            }

            if (schema.TryGetProperty("properties", out var properties))
            {
                foreach (var property in properties.EnumerateObject())
                {
                    if (instance.TryGetProperty(property.Name, out var value))
                    {
                        Check(root, property.Value, value, $"{path}.{property.Name}", violations);
                    }
                }
            }
        }

        if (instance.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            var index = 0;
            foreach (var item in instance.EnumerateArray())
            {
                Check(root, items, item, $"{path}[{index++}]", violations);
            }
        }
    }

    private static bool Allows(JsonElement type, JsonElement instance)
    {
        var allowed = type.ValueKind == JsonValueKind.Array ? type.EnumerateArray().Select(item => item.GetString()!).ToList() : [type.GetString()!];
        return instance.ValueKind switch
        {
            JsonValueKind.Null => allowed.Contains("null"),
            JsonValueKind.Object => allowed.Contains("object"),
            JsonValueKind.Array => allowed.Contains("array"),
            JsonValueKind.String => allowed.Contains("string"),
            JsonValueKind.True or JsonValueKind.False => allowed.Contains("boolean"),
            JsonValueKind.Number => allowed.Contains("number") || (allowed.Contains("integer") && instance.TryGetInt64(out _)),
            _ => false,
        };
    }

    private static JsonElement Resolve(JsonElement root, string pointer)
    {
        if (!pointer.StartsWith('#'))
        {
            throw new NotSupportedException($"Only local references are supported: {pointer}");
        }

        var current = root;
        foreach (var segment in pointer.TrimStart('#').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var name = segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
            current = current.ValueKind == JsonValueKind.Array ? current[int.Parse(name, System.Globalization.CultureInfo.InvariantCulture)] : current.GetProperty(name);
        }

        return current;
    }
}
