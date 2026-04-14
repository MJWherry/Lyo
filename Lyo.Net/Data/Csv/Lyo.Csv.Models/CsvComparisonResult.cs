using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Result of comparing two CSV files.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvComparisonResult
{
    /// <summary>Gets or sets a value indicating whether the files are identical.</summary>
    public bool AreIdentical { get; set; }

    /// <summary>Gets or sets the list of differences found.</summary>
    public List<CsvRowDifference> Differences { get; set; } = new();

    /// <summary>Gets or sets the number of rows in the first file.</summary>
    public long RowCount1 { get; set; }

    /// <summary>Gets or sets the number of rows in the second file.</summary>
    public long RowCount2 { get; set; }

    /// <summary>Gets or sets the number of columns in the first file.</summary>
    public int ColumnCount1 { get; set; }

    /// <summary>Gets or sets the number of columns in the second file.</summary>
    public int ColumnCount2 { get; set; }

    public override string ToString()
        => $"CsvComparisonResult: AreIdentical={AreIdentical}, DifferencesCount={Differences.Count}, RowCount1={RowCount1}, RowCount2={RowCount2}, ColumnCount1={ColumnCount1}, ColumnCount2={ColumnCount2}";
}