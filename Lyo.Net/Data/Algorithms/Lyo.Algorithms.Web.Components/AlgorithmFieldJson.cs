using System.Globalization;
using System.Text.Json;
using Lyo.Parameters;

namespace Lyo.Algorithms.Web.Components;

/// <summary>Maps an algorithm alias onto the CLR type name and JSON that <c>LyoParameterValueField</c> edits.</summary>
public static class AlgorithmFieldJson
{
    private static readonly Dictionary<string, string> ClrNames = new(StringComparer.OrdinalIgnoreCase) {
        ["bool"] = "System.Boolean",
        ["byte"] = "System.Byte",
        ["sbyte"] = "System.SByte",
        ["short"] = "System.Int16",
        ["ushort"] = "System.UInt16",
        ["int"] = "System.Int32",
        ["uint"] = "System.UInt32",
        ["long"] = "System.Int64",
        ["ulong"] = "System.UInt64",
        ["float"] = "System.Single",
        ["double"] = "System.Double",
        ["decimal"] = "System.Decimal",
        ["string"] = "System.String",
        ["datetime"] = "System.DateTime",
        ["datetimeoffset"] = "System.DateTimeOffset",
        ["timespan"] = "System.TimeSpan"
    };

    /// <summary>CLR type name for <paramref name="alias" />. An unknown alias, including a host enum, is <c>System.String</c>. A trailing <c>[]</c> is a one-level array.</summary>
    public static string ClrTypeName(string? alias)
    {
        var text = string.IsNullOrWhiteSpace(alias) ? "double" : alias.Trim();
        var list = text.EndsWith("[]", StringComparison.Ordinal);
        var element = list ? text[..^2] : text;
        var full = ClrNames.TryGetValue(element, out var name) ? name : "System.String";
        return list ? full + "[]" : full;
    }

    /// <summary>JSON for the value field. Blank author text stays unset.</summary>
    public static string? ToField(string? alias, string? text)
    {
        var value = text?.Trim() ?? string.Empty;
        if (value.Length == 0)
            return null;

        var name = string.IsNullOrWhiteSpace(alias) ? "double" : alias.Trim();
        if (name.EndsWith("[]", StringComparison.Ordinal))
            return value.StartsWith('[') ? value : "[]";

        if (name.Equals("bool", StringComparison.OrdinalIgnoreCase))
            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";

        if (AlgorithmDraftRules.IsNumericAlias(name))
            return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? value : null;

        return JsonSerializer.Serialize(text);
    }

    /// <summary>Author text from a value field's JSON.</summary>
    public static string FromField(string? alias, string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return string.Empty;

        var name = string.IsNullOrWhiteSpace(alias) ? "double" : alias.Trim();
        if (name.EndsWith("[]", StringComparison.Ordinal) || name.Equals("bool", StringComparison.OrdinalIgnoreCase) || AlgorithmDraftRules.IsNumericAlias(name))
            return json;

        try {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.String ? document.RootElement.GetString() ?? string.Empty : json;
        }
        catch (JsonException) {
            return json;
        }
    }
}

/// <summary>Parameter definition for one algorithm alias. Job-only fields stay off.</summary>
internal sealed class AlgorithmParameterDefinition(string key, string type, string? value) : ILyoParameterDefinition
{
    public string Key { get; } = key;

    public string? Value { get; } = value;

    public string? Description => null;

    public string Type { get; } = type;

    public byte[]? EncryptedValue => null;

    public bool Required => false;

    public string? ValidationRegex => null;

    public int? MinLength => null;

    public int? MaxLength => null;

    public string? AllowedValues => null;

    public string? Options => null;

    public LyoParameterDefaultKind DefaultKind => LyoParameterDefaultKind.Literal;

    public string? DefaultTemplate => null;
}
