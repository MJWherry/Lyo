using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Outcome of comparing two CSV files.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvComparisonResult
{
    /// <summary>True when the files are identical.</summary>
    public bool AreIdentical { get; set; }

    /// <summary>Differences found.</summary>
    public List<CsvRowDifference> Differences { get; set; } = new();

    /// <summary>Row count in the first file.</summary>
    public long RowCount1 { get; set; }

    /// <summary>Row count in the second file.</summary>
    public long RowCount2 { get; set; }

    /// <summary>Column count in the first file.</summary>
    public int ColumnCount1 { get; set; }

    /// <summary>Column count in the second file.</summary>
    public int ColumnCount2 { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => $"CsvComparisonResult: AreIdentical={AreIdentical}, DifferencesCount={Differences.Count}, RowCount1={RowCount1}, RowCount2={RowCount2}, ColumnCount1={ColumnCount1}, ColumnCount2={ColumnCount2}";
}