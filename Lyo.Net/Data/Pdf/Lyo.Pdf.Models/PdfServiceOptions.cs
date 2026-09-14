using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Pdf.Models;

/// <summary>Tunable defaults for PDF load, layout grouping, and table/key-value extraction.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PdfServiceOptions
{
    public const string SectionName = "PdfServiceOptions";
    public const long SuggestedMaxPdfSizeBytes = 25L * 1024 * 1024;

    /// <summary>Words whose vertical mid-points differ by at most this many points share a line. Starts as 5.0.</summary>
    public double DefaultYTolerance { get; set; } = 5.0;

    /// <summary>Smallest gap (points) allowed between a key's right edge and a value word's left edge. Starts as 0.0.</summary>
    public double DefaultKeyValueGap { get; set; } = 0.0;

    /// <summary>Largest gap (points) between two lines that still merge into one multi-line table header. Starts as 20.0.</summary>
    public double TableHeaderMergeThreshold { get; set; } = 20.0;

    /// <summary>Fraction of header labels that must appear before a line is treated as a header row. Starts as 0.75 (75%).</summary>
    public double TableHeaderMatchThreshold { get; set; } = 0.75;

    /// <summary>X-position slack (points) when assigning words to table columns. Starts as 5.0.</summary>
    public double TableColumnXTolerance { get; set; } = 5.0;

    /// <summary>Smallest fraction (0–1) of a word's area that must overlap the region to keep it. Starts as 0.8 (80%).</summary>
    public double BoundingBoxOverlapThreshold { get; set; } = 0.8;

    /// <summary>Largest Y gap (points) between a key and continuation words on later lines. Words farther away belong to another section. Starts as 10.0.</summary>
    public double MaxContinuationYGap { get; set; } = 10.0;

    /// <summary>
    /// Largest X distance (points) from a value column's left edge for continuation words. Words farther horizontally are not treated as part of the value. Starts as 100.0.
    /// </summary>
    public double MaxContinuationXDistance { get; set; } = 100.0;

    /// <summary>X-position slack (points) when matching continuation words to the value column. Starts as 20.0.</summary>
    public double ValueColumnXTolerance { get; set; } = 20.0;

    /// <summary>
    /// Largest vertical distance (points) from a key line to the first value line for stacked key/value pairs (covers label-to-field gaps and form-field overlays). Starts as 120.0.
    /// </summary>
    public double KeyValueStackedMaxFirstGap { get; set; } = 120.0;

    /// <summary>Optional per-PDF size cap in bytes. When missing or &lt;= 0, <see cref="SuggestedMaxPdfSizeBytes" /> is used.</summary>
    public long? MaxPdfSizeBytes { get; set; }

    /// <summary>When true, PDF operations emit metrics. Starts as false.</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Throws when layout thresholds or size caps are invalid.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfLessThan(DefaultYTolerance, 0);
        ArgumentHelpers.ThrowIfLessThan(DefaultKeyValueGap, 0);
        ArgumentHelpers.ThrowIfLessThan(TableHeaderMergeThreshold, 0);
        ArgumentHelpers.ThrowIfNotInRange(TableHeaderMatchThreshold, 0, 1);
        ArgumentHelpers.ThrowIfLessThan(TableColumnXTolerance, 0);
        ArgumentHelpers.ThrowIfNotInRange(BoundingBoxOverlapThreshold, 0, 1);
        ArgumentHelpers.ThrowIfLessThan(MaxContinuationYGap, 0);
        ArgumentHelpers.ThrowIfLessThan(MaxContinuationXDistance, 0);
        ArgumentHelpers.ThrowIfLessThan(ValueColumnXTolerance, 0);
        ArgumentHelpers.ThrowIfLessThan(KeyValueStackedMaxFirstGap, 0);
        if (MaxPdfSizeBytes is { } maxBytes)
            ArgumentHelpers.ThrowIfNegativeOrZero(maxBytes);
    }

    public override string ToString()
        => $"PdfServiceOptions: DefaultYTolerance={DefaultYTolerance}, DefaultKeyValueGap={DefaultKeyValueGap}, BoundingBoxOverlapThreshold={BoundingBoxOverlapThreshold}, TableHeaderMergeThreshold={TableHeaderMergeThreshold}, TableHeaderMatchThreshold={TableHeaderMatchThreshold}, TableColumnXTolerance={TableColumnXTolerance}, MaxContinuationYGap={MaxContinuationYGap}, MaxContinuationXDistance={MaxContinuationXDistance}, ValueColumnXTolerance={ValueColumnXTolerance}, KeyValueStackedMaxFirstGap={KeyValueStackedMaxFirstGap}, MaxPdfSizeBytes={MaxPdfSizeBytes}, EnableMetrics={EnableMetrics}";
}