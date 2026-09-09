using System.Diagnostics;
using System.Text;

namespace Lyo.Csv.Models;

/// <summary>Stats and metadata about a CSV file.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvStatistics
{
    /// <summary>Total rows in the CSV file (excluding header).</summary>
    public long RowCount { get; set; }

    /// <summary>Column count in the CSV file.</summary>
    public int ColumnCount { get; set; }

    /// <summary>Column headers.</summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>Map of column indices to inferred data types.</summary>
    public Dictionary<int, Type> InferredColumnTypes { get; set; } = new();

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Detected encoding of the file.</summary>
    public Encoding DetectedEncoding { get; set; } = Encoding.UTF8;

    /// <summary>Detected delimiter character.</summary>
    public char? DetectedDelimiter { get; set; }

    /// <summary>Whether the file has a header row.</summary>
    public bool HasHeaderRow { get; set; }

    /// <summary>Sample data from the first few rows (for preview).</summary>
    public List<Dictionary<string, string>> SampleRows { get; set; } = new();

    /// <inheritdoc />
    public override string ToString()
        => $"CsvStatistics: RowCount={RowCount}, ColumnCount={ColumnCount}, FileSizeBytes={FileSizeBytes}, DetectedEncoding={DetectedEncoding.WebName}, DetectedDelimiter='{DetectedDelimiter?.ToString() ?? "N/A"}', HasHeaderRow={HasHeaderRow}, HeadersCount={Headers.Count}, InferredColumnTypesCount={InferredColumnTypes.Count}, SampleRowsCount={SampleRows.Count}";
}