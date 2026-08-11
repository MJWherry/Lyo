using System.Text.Json;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Parameters;

/// <summary>Serialize / deserialize <see cref="ParameterOptions" /> for the definition parameter <c>Options</c> column.</summary>
public static class ParameterOptionsJson
{
    private static readonly JsonSerializerOptions SerializerOptions = LyoJsonSerializerOptions.Create();

    /// <summary>Serializes <paramref name="options" /> to camelCase JSON, or returns null when <paramref name="options" /> is null.</summary>
    public static string? Serialize(ParameterOptions? options)
    {
        if (options is null)
            return null;

        return JsonSerializer.Serialize(options, SerializerOptions);
    }

    /// <summary>Parses JSON into <see cref="ParameterOptions" />. Returns null for null/whitespace. Throws on invalid JSON.</summary>
    public static ParameterOptions? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<ParameterOptions>(json, SerializerOptions);
    }

    /// <summary>Parses JSON; returns false (and null options) when empty or invalid.</summary>
    public static bool TryDeserialize(string? json, out ParameterOptions? options)
    {
        options = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try {
            options = JsonSerializer.Deserialize<ParameterOptions>(json, SerializerOptions);
            return options is not null;
        }
        catch (JsonException) {
            return false;
        }
    }

    /// <summary>Reads <see cref="ParameterOptions.Kind" /> from Options JSON, or null when empty/invalid.</summary>
    public static ParameterOptionsKind? TryGetKind(string? json)
        => TryDeserialize(json, out var options) && options is not null ? options.Kind : null;

    /// <summary>
    /// Builds default Options JSON for a kind selection in the definition editor (static placeholder item or root query template).
    /// Returns null when <paramref name="kind" /> is null.
    /// </summary>
    public static string? CreateDefaultForKind(ParameterOptionsKind? kind)
    {
        if (kind is null)
            return null;

        if (kind == ParameterOptionsKind.Static) {
            return Serialize(
                new ParameterOptions {
                    Kind = ParameterOptionsKind.Static,
                    Items = [new ParameterOptionsItem("key", "label")]
                });
        }

        return Serialize(
            new ParameterOptions {
                Kind = ParameterOptionsKind.Query,
                Query = new QueryReq {
                    From = new FromClause { Alias = "c", EntityType = "" },
                    Select = ["c.Id", "c.Name"],
                    ComputedFields = [new ComputedField("Key", "{c.Id}"), new ComputedField("Value", "{c.Name}")],
                    Amount = 200,
                    Options = new() { TotalCountMode = QueryTotalCountMode.None }
                }
            });
    }

    /// <summary>Validates a deserialized options document has the fields required for its <see cref="ParameterOptions.Kind" />.</summary>
    public static void Validate(ParameterOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        switch (options.Kind) {
            case ParameterOptionsKind.Static:
                if (options.Items.Count == 0)
                    throw new ArgumentException("Static parameter options require at least one item.", nameof(options));
                break;
            case ParameterOptionsKind.Query:
                if (options.Query is null)
                    throw new ArgumentException("Query parameter options require a QueryReq template.", nameof(options));
                if (string.IsNullOrWhiteSpace(options.Query.From.EntityType))
                    throw new ArgumentException("Query parameter options require From.EntityType.", nameof(options));
                if (options.Query.Select.Count == 0)
                    throw new ArgumentException("Query parameter options require at least one Select path.", nameof(options));
                break;
            default:
                throw new ArgumentException($"Unknown parameter options kind '{options.Kind}'.", nameof(options));
        }
    }
}
