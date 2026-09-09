using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;

namespace Lyo.Formatter;

/// <summary>
/// Non-CLR Lyo type for SmartFormat templates. Stored FullName is <see cref="FullName" />; JSON values are strings such as <c>"{DateTime.UtcNow}"</c> or <c>"{-}"</c>.
/// Job/report scheduler and worker render this type the same way as <see cref="LyoTypeInfo.String" />.
/// </summary>
public static class FormatterLyoType
{
    /// <summary>Catalog type name for formatter templates (not a CLR <see cref="Type.FullName" />).</summary>
    public const string FullName = "Lyo.Formatter.Template";

    /// <summary>Catalog entry. Inserted the first time this type is accessed or <see cref="EnsureRegistered" /> runs.</summary>
    public static readonly LyoTypeInfo Template = LyoTypeInfo.Register(
        shortName: "formatter",
        fullName: FullName,
        description: "SmartFormat template (DateTime, Guid, and other values via {tokens})",
        category: LyoTypeCategory.Text,
        editorKind: LyoTypeEditorKind.Formatter,
        defaultJson: "\"\"",
        isClr: false,
        aliases: ["Format", "SmartFormat", "template", "Formatter"]);

    /// <summary>Puts <see cref="Template" /> in the type catalog. Invoked from <c>AddFormatterService</c>.</summary>
    public static LyoTypeInfo EnsureRegistered() => Template;

    /// <summary>True when job/report parameter values of this type go through <see cref="IFormatterService" /> (CLR string or a formatter editor kind).</summary>
    public static bool IsFormattable(LyoTypeInfo? info)
        => info is not null && (info == LyoTypeInfo.String || info.EditorKind == LyoTypeEditorKind.Formatter);

    /// <summary>True when job/report values for <paramref name="typeName" /> are treated as SmartFormat templates.</summary>
    public static bool IsFormattableName(string? typeName) => IsFormattable(LyoTypeInfo.FromName(typeName));
}
