using System.Globalization;
using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;

namespace Lyo.Parameters;

/// <summary>
/// Converts loose parameter text into the JSON a declared type accepts. Reporting stores JSON-encoded values while jobs store what the user typed, so both spellings of a scalar
/// land in the same place: <c>2026-01-01</c> and <c>"2026-01-01"</c> are both a legitimate <c>DateTime</c>.
/// </summary>
/// <remarks>
/// <see cref="LyoParameterValidator" /> and <see cref="LyoParameterDefaults" /> both go through here, so a rendered expression default is stored in the same spelling the
/// validator will accept.
/// </remarks>
public static class LyoParameterValueJson
{
    /// <summary>
    /// True when <paramref name="value" /> holds something the declared type can accept, allowing a scalar that is not valid JSON a second chance as a JSON string literal.
    /// Structured types (JSON trees, collections, XML, formatter templates) get no such leniency: quoting their payload would turn malformed input into a valid string.
    /// </summary>
    /// <param name="type">Declared parameter type.</param>
    /// <param name="value">Non-empty value as supplied.</param>
    public static bool IsAssignable(string? type, string? value)
    {
        if (LyoTypeInfo.TryValidateJson(type, value))
            return true;

        return AllowsScalarLeniency(type) && LyoTypeInfo.TryValidateJson(type, JsonSerializer.Serialize(value));
    }

    /// <summary>
    /// JSON for <paramref name="text" /> under the declared type: the text unchanged when it already parses, otherwise quoted as a JSON string for scalar types, and failing that
    /// parsed as the declared type and re-serialized. Returns null for blank text so an empty render stays unset instead of becoming <c>""</c>.
    /// </summary>
    /// <param name="type">Declared parameter type.</param>
    /// <param name="text">Raw text, typically rendered from a template or typed by a user.</param>
    public static string? Normalize(string? type, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (LyoTypeInfo.TryValidateJson(type, text))
            return text;

        if (!AllowsScalarLeniency(type))
            return text;

        var quoted = JsonSerializer.Serialize(text);
        if (LyoTypeInfo.TryValidateJson(type, quoted))
            return quoted;

        // A template renders through ToString(), so it lands in whatever the current culture and format specifier spell: {DateTime.Now} gives "8/26/2026 10:33:14 AM" and
        // {Count:N0} gives "1,234". Both are the value the author meant, and neither is JSON. Parse as the declared type and re-serialize, instead of making every author
        // reverse-engineer the wire format.
        var info = LyoTypeInfo.FromName(type);
        if (TypeConversion.TryConvertTo(text, info.Type, out var typed, true) && typed is not null)
            return info.ToJson(typed);

        return TryParseFormattedNumber(info, text!, out var number) ? info.ToJson(number) : quoted;
    }

    /// <summary>Removes JSON quoting so length and pattern checks see what the user typed. Non-string types are left as stored.</summary>
    /// <param name="type">Declared parameter type.</param>
    /// <param name="value">Value as supplied.</param>
    public static string Unwrap(string? type, string value)
    {
        if (LyoTypeInfo.FromName(type) != LyoTypeInfo.String)
            return value;

        try {
            return JsonSerializer.Deserialize<string>(value) ?? value;
        }
        catch (JsonException) {
            return value;
        }
    }

    /// <summary>
    /// Parses a number that carries display formatting — group separators and a currency symbol, as <c>{Count:N0}</c> and <c>{Total:C}</c> produce. Percent-formatted text is
    /// deliberately not handled: <c>{Rate:P}</c> renders <c>12.34 %</c> for a stored <c>0.1234</c>, so accepting it would quietly store a value 100 times too large.
    /// </summary>
    /// <param name="info">Declared type.</param>
    /// <param name="text">Rendered text.</param>
    /// <param name="value">Parsed number, boxed as <paramref name="info" />'s CLR type.</param>
    private static bool TryParseFormattedNumber(LyoTypeInfo info, string text, out object? value)
    {
        value = null;
        if (info.Category is not (LyoTypeCategory.Integer or LyoTypeCategory.Number))
            return false;

        const NumberStyles styles = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
        if (!decimal.TryParse(text, styles, CultureInfo.CurrentCulture, out var number) && !decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out number))
            return false;

        // Truncating would store a different number than the template rendered, so an integral parameter accepts only a whole one.
        if (info.Category == LyoTypeCategory.Integer && decimal.Truncate(number) != number)
            return false;

        try {
            value = Convert.ChangeType(number, info.Type, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is OverflowException or InvalidCastException or FormatException) {
            return false;
        }
    }

    private static bool AllowsScalarLeniency(string? type)
    {
        var info = LyoTypeInfo.FromName(type);
        return info.IsScalar
            && info.EditorKind is not (LyoTypeEditorKind.JsonObject or LyoTypeEditorKind.JsonArray or LyoTypeEditorKind.Collection or LyoTypeEditorKind.Xml
                or LyoTypeEditorKind.Formatter);
    }
}
