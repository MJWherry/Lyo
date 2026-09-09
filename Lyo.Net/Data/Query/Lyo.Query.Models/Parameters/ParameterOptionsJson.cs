using Lyo.Parameters;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Common.Json;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Parameters;

/// <summary>Serialize and deserialize <see cref="ParameterOptions" /> for the definition parameter <c>Options</c> column.</summary>
public static class ParameterOptionsJson
{
    private static readonly JsonSerializerOptions SerializerOptions = LyoJsonSerializerOptions.Create();

    /// <summary>Same <c>schema.func</c> grammar as the PostgreSQL sproc runner.</summary>
    private static readonly Regex SprocNameRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>True when <paramref name="storedProcName" /> is a qualified <c>schema.func</c> identifier.</summary>
    public static bool IsValidStoredProcName(string? storedProcName)
        => !string.IsNullOrWhiteSpace(storedProcName) && SprocNameRegex.IsMatch(storedProcName.Trim());

    /// <summary>Serializes <paramref name="options" /> to camelCase JSON, or null when <paramref name="options" /> is null.</summary>
    public static string? Serialize(ParameterOptions? options)
    {
        if (options is null)
            return null;

        return JsonSerializer.Serialize(options, SerializerOptions);
    }

    /// <summary>Parses JSON into <see cref="ParameterOptions" />. Null for null/whitespace. Throws on invalid JSON.</summary>
    public static ParameterOptions? Deserialize(string? json)
    {
        if (json.IsNullOrWhitespace())
            return null;

        return JsonSerializer.Deserialize<ParameterOptions>(json!, SerializerOptions);
    }

    /// <summary>Parses JSON; false (and null options) when empty or invalid.</summary>
    public static bool TryDeserialize(string? json, out ParameterOptions? options)
    {
        options = null;
        if (json.IsNullOrWhitespace())
            return false;

        try {
            options = JsonSerializer.Deserialize<ParameterOptions>(json!, SerializerOptions);
            return options is not null;
        }
        catch (JsonException) {
            return false;
        }
    }

    /// <summary>Reads <see cref="ParameterOptions.Kind" /> from Options JSON, or null when empty or invalid.</summary>
    public static ParameterOptionsKind? TryGetKind(string? json) => TryDeserialize(json, out var options) && options is not null ? options.Kind : null;

    /// <summary>
    /// Builds default Options JSON for a kind pick in the definition editor (static placeholder item or root query template). Null when <paramref name="kind" /> is
    /// null.
    /// </summary>
    public static string? CreateDefaultForKind(ParameterOptionsKind? kind)
    {
        if (kind is null)
            return null;

        if (kind == ParameterOptionsKind.Static)
            return Serialize(new() { Kind = ParameterOptionsKind.Static, Items = [new("key", "label")] });

        if (kind == ParameterOptionsKind.Sproc)
            return Serialize(new() { Kind = ParameterOptionsKind.Sproc, StoredProcName = "public.report_rows", KeyField = "Key", LabelField = "Value" });

        return Serialize(
            new() {
                Kind = ParameterOptionsKind.Query,
                Query = new() {
                    From = new() { Alias = "c", EntityType = "" },
                    Select = ["c.Id", "c.Name"],
                    ComputedFields = [new("Key", "{c.Id}"), new("Value", "{c.Name}")],
                    Amount = 200,
                    Options = new() { TotalCountMode = QueryTotalCountMode.None }
                }
            });
    }

    /// <summary>Static Options JSON whose keys/labels come from the AllowedValues list (legacy chip editor).</summary>
    public static string? FromAllowedValues(string? allowedValues)
    {
        var keys = ParameterListJson.Parse(allowedValues);
        if (keys.Count == 0)
            return null;

        return Serialize(new() { Kind = ParameterOptionsKind.Static, Items = keys.Select(k => new ParameterOptionsItem(k, k)).ToList() });
    }

    /// <summary>JSON-array AllowedValues from static Options keys. Null for query Options or empty documents.</summary>
    public static string? ToAllowedValues(string? optionsJson, ParameterListJsonKind kind = ParameterListJsonKind.String)
    {
        if (!TryDeserialize(optionsJson, out var options) || options is null || options.Kind != ParameterOptionsKind.Static)
            return null;

        return ParameterListJson.Serialize(options.Items.Select(i => i.Key), kind);
    }

    /// <summary>Checks a deserialized options document has the fields required for its <see cref="ParameterOptions.Kind" />.</summary>
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
            case ParameterOptionsKind.Sproc:
                if (!IsValidStoredProcName(options.StoredProcName))
                    throw new ArgumentException("Sproc parameter options require a qualified schema.func name.", nameof(options));

                break;
            default:
                throw new ArgumentException($"Unknown parameter options kind '{options.Kind}'.", nameof(options));
        }
    }
}