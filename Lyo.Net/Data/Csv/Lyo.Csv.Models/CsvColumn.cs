using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Represents a column definition in a CSV schema.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvColumn
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the expected data type for this column. If null, type checking is skipped.</summary>
    public Type? ExpectedType { get; set; }

    /// <summary>Gets or sets a value indicating whether this column is required.</summary>
    public bool Required { get; set; }

    /// <summary>Gets or sets an optional custom validator function for this column.</summary>
    public Func<string, bool>? Validator { get; set; }

    /// <summary>Gets or sets an optional error message to use when validation fails.</summary>
    public string? ValidationErrorMessage { get; set; }

    public CsvColumn() { }

    public CsvColumn(string name, bool required, Func<string, bool>? validator)
    {
        Name = name;
        Required = required;
        Validator = validator;
    }

    public override string ToString()
        => $"CsvColumn: Name='{Name}', ExpectedType='{ExpectedType?.Name ?? "Any"}', Required={Required}, HasValidator={Validator != null}, ValidationErrorMessage='{ValidationErrorMessage}'";
}