using System.Diagnostics;

namespace Lyo.Pdf.Models;

/// <summary>Options for configuring PDF service behavior.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PdfServiceOptions
{
    public const string SectionName = "PdfServiceOptions";
    public const long SuggestedMaxPdfSizeBytes = 25L * 1024 * 1024;
    public const long SuggestedMaxTotalLoadedBytes = 256L * 1024 * 1024;

    /// <summary>Two words whose vertical mid-points are within this many points are considered to be on the same line. Default: 5.0</summary>
    public double DefaultYTolerance { get; set; } = 5.0;

    /// <summary>Minimum gap (points) between a key's right edge and a value word's left edge. Default: 0.0</summary>
    public double DefaultKeyValueGap { get; set; } = 0.0;

    /// <summary>Maximum gap (points) between two lines when detecting multi-line table headers. Default: 20.0</summary>
    public double TableHeaderMergeThreshold { get; set; } = 20.0;

    /// <summary>Minimum percentage of header labels that must be found to consider a line as a header row. Default: 0.75 (75%)</summary>
    public double TableHeaderMatchThreshold { get; set; } = 0.75;

    /// <summary>Tolerance (points) for matching words to table columns based on X position. Default: 5.0</summary>
    public double TableColumnXTolerance { get; set; } = 5.0;

    /// <summary>Minimum fraction (0–1) of a word's area that must overlap the bounding box region to be included. Default: 0.8 (80%).</summary>
    public double BoundingBoxOverlapThreshold { get; set; } = 0.8;

    /// <summary>Maximum Y gap (points) between a key and continuation words on subsequent lines. Words beyond this distance are considered part of a different section. Default: 10.0</summary>
    public double MaxContinuationYGap { get; set; } = 10.0;

    /// <summary>
    /// Maximum X distance (points) from a value column's left edge for continuation words. Words beyond this distance horizontally are not considered part of the value. Default:
    /// 100.0
    /// </summary>
    public double MaxContinuationXDistance { get; set; } = 100.0;

    /// <summary>Tolerance (points) for matching continuation words to the value column X position. Default: 20.0</summary>
    public double ValueColumnXTolerance { get; set; } = 20.0;

    /// <summary>
    /// Optional per-PDF max size in bytes. If not set or <= 0, SuggestedMaxPdfSizeBytes is used.</summary>
    public long? MaxPdfSizeBytes { get; set; }

    /// <summary>
    /// Optional max total loaded PDF bytes in memory. If not set or <= 0, SuggestedMaxTotalLoadedBytes is used.</summary>
    public long? MaxTotalLoadedBytes { get; set; }

    /// <summary>Enable metrics collection for PDF operations. Default: false.</summary>
    public bool EnableMetrics { get; set; } = false;

    public override string ToString()
        => $"PdfServiceOptions: DefaultYTolerance={DefaultYTolerance}, DefaultKeyValueGap={DefaultKeyValueGap}, BoundingBoxOverlapThreshold={BoundingBoxOverlapThreshold}, TableHeaderMergeThreshold={TableHeaderMergeThreshold}, TableHeaderMatchThreshold={TableHeaderMatchThreshold}, TableColumnXTolerance={TableColumnXTolerance}, MaxContinuationYGap={MaxContinuationYGap}, MaxContinuationXDistance={MaxContinuationXDistance}, ValueColumnXTolerance={ValueColumnXTolerance}, MaxPdfSizeBytes={MaxPdfSizeBytes}, MaxTotalLoadedBytes={MaxTotalLoadedBytes}, EnableMetrics={EnableMetrics}";
}