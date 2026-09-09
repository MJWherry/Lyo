using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Progress snapshot for CSV operations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvProgress
{
    /// <summary>Rows processed so far.</summary>
    public long RowsProcessed { get; set; }

    /// <summary>Total rows to process. May be 0 if the total is unknown.</summary>
    public long TotalRows { get; set; }

    /// <summary>Percent complete (0-100). 0 if TotalRows is 0 or unknown.</summary>
    public double Percentage => TotalRows > 0 ? RowsProcessed / (double)TotalRows * 100 : 0;

    /// <summary>Current operation description.</summary>
    public string? Operation { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"CsvProgress: RowsProcessed={RowsProcessed}, TotalRows={TotalRows}, Percentage={Percentage:F2}%, Operation='{Operation ?? "N/A"}'";
}