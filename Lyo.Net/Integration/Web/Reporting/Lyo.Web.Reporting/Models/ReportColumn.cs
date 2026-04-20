using System.Diagnostics;

namespace Lyo.Web.Reporting.Models;

/// <summary>Represents a column layout within a report section.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportColumn
{
    /// <summary>Gets or sets the column label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the column value.</summary>
    public object? Value { get; set; }

    /// <summary>Gets or sets the column width (as CSS value, e.g., "50%", "200px").</summary>
    public string? Width { get; set; }

    /// <summary>Gets or sets the column alignment (left, right, center, justify).</summary>
    public string? Alignment { get; set; }

    /// <summary>Gets or sets whether this column should be emphasized.</summary>
    public bool Emphasized { get; set; }

    /// <summary>Gets or sets custom CSS styles for this column.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    /// <summary>Gets or sets a formatter function for the value (as string representation).</summary>
    public Func<object?, string>? ValueFormatter { get; set; }

    public override string ToString() => $"Column: {Label ?? "(No Label)"} = {Value}";
}