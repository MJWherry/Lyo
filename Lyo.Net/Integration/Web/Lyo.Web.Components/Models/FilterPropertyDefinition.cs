using Lyo.Web.Components.UniqueValueSelector;

namespace Lyo.Web.Components.Models;

public class FilterPropertyDefinition(string propertyName, string? displayName = null, FilterPropertyType type = FilterPropertyType.String)
{
    public string PropertyName { get; set; } = propertyName;

    public string? DisplayName { get; set; } = displayName;

    public FilterPropertyType Type { get; set; } = type;

    public Dictionary<string, string>? EnumValues { get; set; }

    public IReadOnlyList<SpUniqueValueCount>? UniqueValues { get; set; }

    public Func<object?, string>? ValueFormatter { get; set; }

    public string? Schema { get; set; }

    public string? Table { get; set; }

    public string? Column { get; set; }

    public bool HasDynamicUniqueValues => !string.IsNullOrEmpty(Schema) && !string.IsNullOrEmpty(Table) && !string.IsNullOrEmpty(Column);

    public static FilterPropertyDefinition FromEnum(
        string propertyName,
        Dictionary<string, string> enumValues,
        FilterPropertyType type = FilterPropertyType.String,
        string? displayName = null)
        => new(propertyName, displayName) { Type = type, EnumValues = enumValues };

    public static FilterPropertyDefinition FromDatabase(string propertyName, string column, string table, string? schema = "public", string? displayName = null)
        => new(propertyName, displayName) { Schema = schema, Table = table, Column = column };
}