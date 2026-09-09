using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>One column definition in a CSV schema.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvColumn
{
    /// <summary>Column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Expected data type for this column. If null, type checking is skipped.</summary>
    public Type? ExpectedType { get; set; }

    /// <summary>If true, this column is required.</summary>
    public bool Required { get; set; }

    /// <summary>Optional custom validator for this column.</summary>
    public Func<string, bool>? Validator { get; set; }

    /// <summary>Optional error message when validation fails.</summary>
    public string? ValidationErrorMessage { get; set; }

    /// <summary>Builds an empty column definition.</summary>
    public CsvColumn() { }

    /// <summary>Builds a column with name, required flag, and optional validator.</summary>
    public CsvColumn(string name, bool required, Func<string, bool>? validator)
    {
        Name = name;
        Required = required;
        Validator = validator;
    }

    /// <inheritdoc />
    public override string ToString()
        => $"CsvColumn: Name='{Name}', ExpectedType='{ExpectedType?.Name ?? "Any"}', Required={Required}, HasValidator={Validator != null}, ValidationErrorMessage='{ValidationErrorMessage}'";
}