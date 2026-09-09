namespace Lyo.Csv.Models;

/// <summary>Maps a property to a CSV column header, or excludes it from typed read/write. When omitted, the property name is used (case-insensitive match on read).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CsvColumnAttribute : Attribute
{
    /// <summary>CSV header name. When null or empty and not <see cref="Ignore" />, the property name is used.</summary>
    public string? Name { get; }

    /// <summary>If true, the property is excluded from typed CSV mapping.</summary>
    public bool Ignore { get; set; }

    /// <summary>Builds a column mapping with the given header <paramref name="name" />.</summary>
    public CsvColumnAttribute(string name) => Name = name;

    /// <summary>Builds a column mapping that optionally ignores the property.</summary>
    public CsvColumnAttribute() { }
}