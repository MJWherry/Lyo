using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Represents an error that occurred during CSV parsing.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvParseError
{
    /// <summary>Gets or sets the row number where the error occurred (1-based).</summary>
    public int RowNumber { get; set; }

    /// <summary>Gets or sets the raw CSV record that caused the error.</summary>
    public string? RawRecord { get; set; }

    /// <summary>Gets or sets the exception that occurred.</summary>
    public Exception Exception { get; set; } = null!;

    /// <summary>Gets or sets the column index where the error occurred (if applicable).</summary>
    public int? ColumnIndex { get; set; }

    /// <summary>Gets or sets the column name where the error occurred (if applicable).</summary>
    public string? ColumnName { get; set; }

    public override string ToString()
        => $"CsvParseError: RowNumber={RowNumber}, ColumnIndex={ColumnIndex?.ToString() ?? "N/A"}, ColumnName='{ColumnName ?? "N/A"}', RawRecord='{RawRecord ?? "N/A"}', Exception='{Exception.Message}'";
}