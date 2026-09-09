using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>An error that occurred during CSV parse.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvParseError
{
    /// <summary>Row number where the error occurred (1-based).</summary>
    public int RowNumber { get; set; }

    /// <summary>Raw CSV record that caused the error.</summary>
    public string? RawRecord { get; set; }

    /// <summary>Exception that occurred.</summary>
    public Exception Exception { get; set; } = null!;

    /// <summary>Column index where the error occurred (if applicable).</summary>
    public int? ColumnIndex { get; set; }

    /// <summary>Column name where the error occurred (if applicable).</summary>
    public string? ColumnName { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => $"CsvParseError: RowNumber={RowNumber}, ColumnIndex={ColumnIndex?.ToString() ?? "N/A"}, ColumnName='{ColumnName ?? "N/A"}', RawRecord='{RawRecord ?? "N/A"}', Exception='{Exception.Message}'";
}