using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components.UniqueValueSelector;

namespace Lyo.Web.Components.Models;

/// <summary>Column metadata for query and filter editors. <see cref="Type" /> drives operators and value widgets via <see cref="LyoTypeInfo" />.</summary>
public class FilterPropertyDefinition(string propertyName, string? displayName = null, LyoTypeInfo? type = null)
{
    public string PropertyName { get; set; } = propertyName;

    public string? DisplayName { get; set; } = displayName;

    public LyoTypeInfo Type { get; set; } = type ?? LyoTypeInfo.String;

    public Dictionary<string, string>? EnumValues { get; set; }

    public IReadOnlyList<SpUniqueValueCount>? UniqueValues { get; set; }

    public Func<object?, string>? ValueFormatter { get; set; }

    public string? Schema { get; set; }

    public string? Table { get; set; }

    public string? Column { get; set; }

    public bool HasDynamicUniqueValues => !string.IsNullOrEmpty(Schema) && !string.IsNullOrEmpty(Table) && !string.IsNullOrEmpty(Column);

    /// <summary>Text and identifier columns (contains, starts, ends, equals, in).</summary>
    public bool IsTextLike => Type.Category is LyoTypeCategory.Text or LyoTypeCategory.Identifier;

    /// <summary>Integer or decimal numeric columns.</summary>
    public bool IsNumeric => Type.Category is LyoTypeCategory.Integer or LyoTypeCategory.Number;

    /// <summary>Date, time, and duration columns.</summary>
    public bool IsTemporal => Type.Category == LyoTypeCategory.Temporal;

    /// <summary>Boolean-typed columns.</summary>
    public bool IsBoolean => Type.Category == LyoTypeCategory.Boolean;

    /// <summary>An enum catalog type or a property with <see cref="EnumValues" />.</summary>
    public bool IsEnumType => Type.Category == LyoTypeCategory.Enum || EnumValues != null;

    public static FilterPropertyDefinition FromEnum(
        string propertyName,
        Dictionary<string, string> enumValues,
        LyoTypeInfo? type = null,
        string? displayName = null)
        => new(propertyName, displayName) { Type = type ?? LyoTypeInfo.Enum, EnumValues = enumValues };

    public static FilterPropertyDefinition FromDatabase(string propertyName, string column, string table, string? schema = "public", string? displayName = null)
        => new(propertyName, displayName) { Schema = schema, Table = table, Column = column };
}
