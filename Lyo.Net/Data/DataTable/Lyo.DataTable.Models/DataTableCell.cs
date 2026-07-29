using System.Diagnostics;

namespace Lyo.DataTable.Models;

/// <summary>
/// Cell value with optional merge spans for data tables.
/// Formatting is stored sparsely on <see cref="DataTable" />, not on this type.
/// </summary>
/// <param name="Value">The typed value of the cell.</param>
/// <param name="ColSpan">Number of columns this cell spans (1 = no spanning).</param>
/// <param name="RowSpan">Number of rows this cell spans (1 = no spanning).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DataTableCell<T>(
    T? Value,
    int ColSpan = 1,
    int RowSpan = 1) : IDataTableCell
{
    /// <summary>Empty cell placeholder. Immutable; safe to reuse.</summary>
    public static DataTableCell<string> Empty { get; } = new("");

    /// <summary>The display string for the cell (Value?.ToString() ?? "").</summary>
    public string DisplayValue => Value?.ToString() ?? "";

    /// <summary>Creates a cell with only a value (no formatting; formatting is table-scoped).</summary>
    public static DataTableCell<T> FromValue(T? value) => new(value);

    public override string ToString() => $"({typeof(T).Name}) {DisplayValue})";
}

/// <summary>Static helpers for DataTableCell.</summary>
public static class DataTableCell
{
    /// <summary>Empty cell placeholder.</summary>
    public static IDataTableCell Empty => DataTableCell<string>.Empty;

    /// <summary>Creates a string cell with no formatting.</summary>
    public static IDataTableCell FromValue(string? value) => DataTableCell<string>.FromValue(value ?? "");

    /// <summary>Creates a string cell with the given merge spans.</summary>
    public static IDataTableCell FromValue(string? value, int colSpan, int rowSpan)
        => new DataTableCell<string>(value ?? "", colSpan < 1 ? 1 : colSpan, rowSpan < 1 ? 1 : rowSpan);
}
