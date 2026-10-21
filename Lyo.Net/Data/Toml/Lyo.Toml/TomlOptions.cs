using Lyo.Json;

namespace Lyo.Toml;

/// <summary>
/// Options for <see cref="TomlService" />. The JSON contract fields control names, numbers, and omission. <see cref="JsonOptions.IgnoreNulls" /> must stay true because TOML
/// has no null.
/// </summary>
public sealed class TomlOptions : JsonOptions
{
    /// <summary>Configuration section name. Nothing in this package binds it.</summary>
    public new const string SectionName = "Lyo:Toml";

    /// <summary>Key used for a root array. Starts as <c>items</c>, so a list exports as <c>[[items]]</c>.</summary>
    public string ItemsKey { get; set; } = "items";

    /// <summary>How nested objects are written. Arrays of objects stay arrays of tables.</summary>
    public TomlNestedTables NestedTables { get; set; } = TomlNestedTables.Table;

    /// <summary>How a <see cref="DateTimeOffset" /> is written when <see cref="JsonOptions.DateTimeStyle" /> is not unix milliseconds.</summary>
    public TomlDateStyle DateStyle { get; set; } = TomlDateStyle.Offset;

    /// <summary>Builds options. TOML starts indented.</summary>
    public TomlOptions() => WriteIndented = true;

    /// <summary>Checks option values. <see cref="JsonOptions.IgnoreNulls" /> set to false throws because TOML cannot write null.</summary>
    public new void Validate()
    {
        base.Validate();
        if (!IgnoreNulls)
            throw new ArgumentException("TOML has no null. IgnoreNulls must stay true.");
    }
}

/// <summary>Shape of a nested TOML object.</summary>
public enum TomlNestedTables
{
    /// <summary>Standard tables, <c>[address]</c> and <c>[[items]]</c>.</summary>
    Table,

    /// <summary>Inline tables, <c>address = { city = "London" }</c>. Arrays of objects stay arrays of tables.</summary>
    Inline
}

/// <summary>How a date is written in TOML.</summary>
public enum TomlDateStyle
{
    /// <summary>Offset date-time.</summary>
    Offset,

    /// <summary>Local date-time, without an offset.</summary>
    Local,

    /// <summary>Local date, without a time.</summary>
    LocalDate
}