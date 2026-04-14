using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Represents progress information for CSV operations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvProgress
{
    /// <summary>Gets or sets the number of rows processed so far.</summary>
    public long RowsProcessed { get; set; }

    /// <summary>Gets or sets the total number of rows to process. May be 0 if total is unknown.</summary>
    public long TotalRows { get; set; }

    /// <summary>Gets the percentage complete (0-100). Returns 0 if TotalRows is 0 or unknown.</summary>
    public double Percentage => TotalRows > 0 ? RowsProcessed / (double)TotalRows * 100 : 0;

    /// <summary>Gets or sets the current operation description.</summary>
    public string? Operation { get; set; }

    public override string ToString() => $"CsvProgress: RowsProcessed={RowsProcessed}, TotalRows={TotalRows}, Percentage={Percentage:F2}%, Operation='{Operation ?? "N/A"}'";
}