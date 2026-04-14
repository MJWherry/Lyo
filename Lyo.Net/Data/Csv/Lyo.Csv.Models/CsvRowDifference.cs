using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Represents a difference between two CSV rows.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CsvRowDifference(int RowNumber, DifferenceType Type, string? ColumnName = null, string? Value1 = null, string? Value2 = null)
{
    public override string ToString()
        => $"CsvRowDifference: RowNumber={RowNumber}, Type={Type}, ColumnName='{ColumnName ?? "N/A"}', Value1='{Value1 ?? "N/A"}', Value2='{Value2 ?? "N/A"}'";
}

/// <summary>Type of difference between CSV files.</summary>
public enum DifferenceType
{
    /// <summary>Row exists in first file but not in second.</summary>
    Added,

    /// <summary>Row exists in second file but not in first.</summary>
    Removed,

    /// <summary>Row exists in both files but has different values.</summary>
    Modified
}