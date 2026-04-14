using System.Diagnostics;
using System.Text;

namespace Lyo.Csv.Models;

/// <summary>Statistics and metadata about a CSV file.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvStatistics
{
    /// <summary>Gets or sets the total number of rows in the CSV file (excluding header).</summary>
    public long RowCount { get; set; }

    /// <summary>Gets or sets the number of columns in the CSV file.</summary>
    public int ColumnCount { get; set; }

    /// <summary>Gets or sets the list of column headers.</summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>Gets or sets a dictionary mapping column indices to inferred data types.</summary>
    public Dictionary<int, Type> InferredColumnTypes { get; set; } = new();

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets or sets the detected encoding of the file.</summary>
    public Encoding DetectedEncoding { get; set; } = Encoding.UTF8;

    /// <summary>Gets or sets the detected delimiter character.</summary>
    public char? DetectedDelimiter { get; set; }

    /// <summary>Gets or sets whether the file has a header row.</summary>
    public bool HasHeaderRow { get; set; }

    /// <summary>Gets or sets sample data from the first few rows (for preview).</summary>
    public List<Dictionary<string, string>> SampleRows { get; set; } = new();

    public override string ToString()
        => $"CsvStatistics: RowCount={RowCount}, ColumnCount={ColumnCount}, FileSizeBytes={FileSizeBytes}, DetectedEncoding={DetectedEncoding.WebName}, DetectedDelimiter='{DetectedDelimiter?.ToString() ?? "N/A"}', HasHeaderRow={HasHeaderRow}, HeadersCount={Headers.Count}, InferredColumnTypesCount={InferredColumnTypes.Count}, SampleRowsCount={SampleRows.Count}";
}