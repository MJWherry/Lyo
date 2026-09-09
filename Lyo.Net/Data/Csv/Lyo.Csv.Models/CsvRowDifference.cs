using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>One difference between two CSV rows.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CsvRowDifference(int RowNumber, DifferenceType Type, string? ColumnName = null, string? Value1 = null, string? Value2 = null)
{
    /// <inheritdoc />
    public override string ToString()
        => $"CsvRowDifference: RowNumber={RowNumber}, Type={Type}, ColumnName='{ColumnName ?? "N/A"}', Value1='{Value1 ?? "N/A"}', Value2='{Value2 ?? "N/A"}'";
}

/// <summary>Kind of difference between CSV files.</summary>
public enum DifferenceType
{
    /// <summary>Row exists in the first file but not the second.</summary>
    Added,

    /// <summary>Row exists in the second file but not the first.</summary>
    Removed,

    /// <summary>Row exists in both files but values differ.</summary>
    Modified
}