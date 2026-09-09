using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Parameters;

namespace Lyo.Web.Components.LyoType;

/// <summary>MudBlazor utility methods for <see cref="LyoTypeInfo" /> pickers, chips, and JSON value editors.</summary>
public static class LyoTypeUi
{
    /// <summary>Sentinel FullName for the Custom… picker row (not a real CLR type).</summary>
    public const string CustomTypeToken = "__custom__";

    /// <summary>
    /// Stored type name of the registered template type, or null when no formatter package is loaded. Looked up by editor kind instead of by name so this package does not
    /// depend on the formatter.
    /// </summary>
    public static string? TemplateTypeName => LyoTypeInfo.All.FirstOrDefault(t => t.EditorKind == LyoTypeEditorKind.Formatter)?.FullName;

    /// <summary>Chip color for a stored type name (Regex/Xml stay warning; Formatter is secondary; otherwise <see cref="ForCategory" />).</summary>
    public static Color ForType(string? typeName)
    {
        var info = LyoTypeInfo.FromName(typeName);
        if (info.EditorKind == LyoTypeEditorKind.Formatter)
            return Color.Secondary;
        return info.EditorKind is LyoTypeEditorKind.Regex or LyoTypeEditorKind.Xml ? Color.Warning : ForCategory(info.Category);
    }

    /// <summary>Chip color for a catalog category.</summary>
    public static Color ForCategory(LyoTypeCategory category)
        => category switch {
            LyoTypeCategory.Boolean => Color.Success,
            LyoTypeCategory.Integer or LyoTypeCategory.Number => Color.Info,
            LyoTypeCategory.Temporal => Color.Tertiary,
            LyoTypeCategory.Identifier => Color.Secondary,
            LyoTypeCategory.Enum => Color.Primary,
            LyoTypeCategory.Json or LyoTypeCategory.Binary => Color.Warning,
            LyoTypeCategory.Collection => Color.Info,
            var _ => Color.Default
        };

    /// <summary>Capitalized picker/chip label. Collections draw as <c>List&lt;Int&gt;</c> or <c>Int[]</c>.</summary>
    public static string DisplayName(LyoTypeInfo info)
    {
        if (info.IsCollection && info.ElementType != null && info.Category != LyoTypeCategory.Binary) {
            var element = LyoTypeInfo.FromType(info.ElementType);
            var elName = element == LyoTypeInfo.Enum ? Capitalize(info.ElementType.Name) : DisplayName(element);
            return info.Type.IsArray ? $"{elName}[]" : $"List<{elName}>";
        }

        return Capitalize(info.ShortName);
    }

    /// <summary>Capitalized label for a stored type name, or the raw name when unknown.</summary>
    public static string DisplayName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return "—";

        var info = LyoTypeInfo.FromName(typeName);
        return info == LyoTypeInfo.Unknown ? typeName.Trim() : DisplayName(info);
    }

    /// <summary>Non-collection catalog types for the type dropdown, grouped by category, sorted by <see cref="DisplayName(LyoTypeInfo)" />.</summary>
    public static IEnumerable<(LyoTypeCategory Category, IReadOnlyList<LyoTypeInfo> Types)> PickerGroups()
    {
        foreach (var category in Enum.GetValues<LyoTypeCategory>()) {
            if (category is LyoTypeCategory.Unknown or LyoTypeCategory.Collection)
                continue;

            var types = LyoTypeInfo.ByCategory(category)
                .Where(t => !t.IsCollection)
                .OrderBy(t => DisplayName(t), StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (types.Count > 0)
                yield return (category, types);
        }
    }

    /// <summary>True when <paramref name="info" /> can be wrapped as <c>List&lt;T&gt;</c> or <c>T[]</c> in the picker.</summary>
    public static bool CanWrapInCollection(LyoTypeInfo info)
        => info != LyoTypeInfo.Unknown
            && info != LyoTypeInfo.Enum
            && info.IsClr
            && !info.IsCollection
            && info.Category is LyoTypeCategory.Text or LyoTypeCategory.Boolean or LyoTypeCategory.Integer or LyoTypeCategory.Number
                or LyoTypeCategory.Temporal or LyoTypeCategory.Identifier
            && info.EditorKind is not (LyoTypeEditorKind.Regex or LyoTypeEditorKind.Xml or LyoTypeEditorKind.Formatter);

    /// <summary>List/array shape encoded in a stored FullName. Byte[] stays a scalar binary type.</summary>
    public static LyoTypeCollectionShape CollectionShape(string? typeName)
    {
        var info = LyoTypeInfo.FromName(typeName);
        if (!info.IsCollection || info.Category == LyoTypeCategory.Binary)
            return LyoTypeCollectionShape.Value;

        return info.Type.IsArray ? LyoTypeCollectionShape.Array : LyoTypeCollectionShape.List;
    }

    /// <summary>Element FullName for a list/array stored type; otherwise the type itself when it is a picker scalar.</summary>
    public static string ElementFullName(string? typeName)
    {
        var info = LyoTypeInfo.FromName(typeName);
        if (info.IsCollection && info.ElementType != null && info.Category != LyoTypeCategory.Binary) {
            var element = LyoTypeInfo.FromType(info.ElementType);
            return element == LyoTypeInfo.Enum ? info.ElementType.FullName ?? info.ElementType.Name : element.FullName;
        }

        if (info != LyoTypeInfo.Unknown && info != LyoTypeInfo.Enum && !info.IsCollection)
            return info.FullName;

        return typeName?.Trim() ?? LyoTypeInfo.String.FullName;
    }

    /// <summary>Stored FullName for an element type plus list/array shape.</summary>
    public static string ComposeFullName(string? elementFullName, LyoTypeCollectionShape shape)
    {
        if (string.IsNullOrWhiteSpace(elementFullName) || elementFullName == CustomTypeToken)
            return elementFullName?.Trim() ?? "";

        var element = LyoTypeInfo.FromName(elementFullName);
        if (!CanWrapInCollection(element) || shape == LyoTypeCollectionShape.Value)
            return element == LyoTypeInfo.Unknown ? elementFullName.Trim() : element.FullName;

        var clr = element.Type;
        return shape == LyoTypeCollectionShape.List ? LyoTypeInfo.ListOf(clr).FullName : LyoTypeInfo.ArrayOf(clr).FullName;
    }

    /// <summary>True when the picker needs a concrete CLR FullName (Custom… or Enum).</summary>
    public static bool NeedsConcreteFullName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName) || typeName == CustomTypeToken)
            return true;

        var info = LyoTypeInfo.FromName(typeName);
        return info == LyoTypeInfo.Enum || info == LyoTypeInfo.Unknown;
    }

    /// <summary>Empty string for the CLR FullName field when the stored type is the Enum catalog sentinel.</summary>
    public static string ConcreteFullNameDisplay(string? typeName)
        => string.IsNullOrWhiteSpace(typeName) || typeName == CustomTypeToken
            || string.Equals(typeName.Trim(), LyoTypeInfo.Enum.FullName, StringComparison.Ordinal)
            ? ""
            : typeName.Trim();

    /// <summary>Stores the Enum catalog FullName when the CLR FullName field is cleared while editing an enum.</summary>
    public static string ConcreteFullNameFromInput(string? currentType, string? typed)
    {
        if (!string.IsNullOrWhiteSpace(typed))
            return typed.Trim();

        return LyoTypeInfo.FromName(currentType) == LyoTypeInfo.Enum ? LyoTypeInfo.Enum.FullName : "";
    }

    /// <summary>Regex that rejects chips that cannot parse as the list/array/allowed-value element type.</summary>
    public static (string? Pattern, string? Message) ChipValidation(string? typeName)
    {
        var info = LyoTypeInfo.FromName(typeName);
        var element = info.IsCollection && info.ElementType != null ? LyoTypeInfo.FromType(info.ElementType) : info;
        return element.Category switch {
            LyoTypeCategory.Integer => ("^[+-]?\\d+$", "Enter an integer."),
            LyoTypeCategory.Number => ("^[+-]?(?:\\d+\\.?\\d*|\\.\\d+)$", "Enter a number."),
            LyoTypeCategory.Boolean => ("^(?i:true|false)$", "Enter true or false."),
            LyoTypeCategory.Identifier when element == LyoTypeInfo.Guid => (
                "(?i)^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$", "Enter a GUID."),
            var _ => (null, null)
        };
    }

    /// <summary>True when a picker change should replace stored JSON with <see cref="DefaultJson" /> (catalog identity or list/array shape changed).</summary>
    public static bool ShouldResetValue(string? fromType, string? toType)
    {
        if (string.Equals(fromType, toType, StringComparison.Ordinal))
            return false;

        var from = LyoTypeInfo.FromName(fromType);
        var to = LyoTypeInfo.FromName(toType);
        if (from == LyoTypeInfo.Unknown && to == LyoTypeInfo.Unknown)
            return false;

        if (from == LyoTypeInfo.Enum && to == LyoTypeInfo.Unknown || from == LyoTypeInfo.Unknown && to == LyoTypeInfo.Enum)
            return false;

        return from != to || CollectionShape(fromType) != CollectionShape(toType);
    }

    /// <summary>JSON-array element kind for chip editors and option selects (uses the collection element when <paramref name="typeName" /> is a list/array).</summary>
    public static ParameterListJsonKind ToListKind(string? typeName)
    {
        var info = LyoTypeInfo.FromName(typeName);
        var element = info.IsCollection && info.ElementType != null ? LyoTypeInfo.FromType(info.ElementType) : info;
        return element.Category switch {
            LyoTypeCategory.Integer or LyoTypeCategory.Number => ParameterListJsonKind.Number,
            LyoTypeCategory.Boolean => ParameterListJsonKind.Bool,
            var _ => ParameterListJsonKind.String
        };
    }

    /// <summary>True when the editor should move under the row expander (JSON, XML, collections, binary, regex, formatter, or an unknown/custom CLR type).</summary>
    public static bool IsWideEditor(string? typeName)
    {
        var info = LyoTypeInfo.FromName(typeName);
        if (info == LyoTypeInfo.Unknown)
            return true;

        return info.EditorKind is LyoTypeEditorKind.JsonObject or LyoTypeEditorKind.JsonArray or LyoTypeEditorKind.Xml or LyoTypeEditorKind.Collection
            or LyoTypeEditorKind.Binary or LyoTypeEditorKind.Formatter or LyoTypeEditorKind.Regex;
    }

    /// <summary>Short preview of stored JSON for a collapsed value cell.</summary>
    public static string ValuePreview(string? json, int maxChars = 48)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return "—";

        var trimmed = json.Trim();
        return trimmed.Length <= maxChars ? trimmed : trimmed[..maxChars] + "…";
    }

    /// <summary>SmartFormat token examples (clock / definition). Not injected into pickers. templates are free text.</summary>
    public static readonly IReadOnlyList<string> FormatterTokens = [
        "{DateTime.UtcNow}",
        "{DateTime.Now}",
        "{DateTime.Now.AddDays(-1)}",
        "{Definition.Name}"
    ];

    /// <summary>True when <paramref name="typeName" /> is a catalog collection (or synthesized <c>List&lt;T&gt;</c> / array).</summary>
    public static bool IsCollection(string? typeName) => LyoTypeInfo.FromName(typeName).IsCollection;

    /// <summary>True when <paramref name="typeName" /> is boolean.</summary>
    public static bool IsBoolean(string? typeName) => LyoTypeInfo.FromName(typeName).Category == LyoTypeCategory.Boolean;

    /// <summary>Empty JSON payload for a type name (<c>null</c> JSON for unknown types).</summary>
    public static string DefaultJson(string? typeName)
    {
        var known = LyoTypeInfo.FromName(typeName);
        return known == LyoTypeInfo.Unknown ? "null" : known.DefaultJson;
    }

    private static string Capitalize(string value)
        => string.IsNullOrEmpty(value) || !char.IsLower(value[0]) ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
