namespace Lyo.Pdf.Web.Components.PdfAnnotator;

public enum PdfAnnotationExtractionType
{
    BoundingBoxText,
    KeyValue,
    Table
}

public sealed class LyoPdfAnnotationResult
{
    public string Key { get; init; } = string.Empty;

    public string BoundingBoxSummary { get; init; } = string.Empty;

    public PdfAnnotationExtractionType ExtractionType { get; set; } = PdfAnnotationExtractionType.BoundingBoxText;

    public string ExtractedText { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IReadOnlyDictionary<string, string?>? KeyValuePairs { get; set; }

    public IReadOnlyList<IReadOnlyDictionary<string, string?>>? TableRows { get; set; }

    public List<string> KnownKeys { get; set; } = [];

    public List<string> TableHeaders { get; set; } = [];

    public double YTolerance { get; set; } = 5.0;

    public bool SplitColumns { get; set; }

    /// <summary>
    /// When extraction is <see cref="PdfAnnotationExtractionType.BoundingBoxText" />, splits the region into this many vertical columns (≥ 1). Values &gt; 1 use gutter/band
    /// detection from the PDF layout.
    /// </summary>
    public int ColumnCount { get; set; } = 1;

    /// <summary>Populated when <see cref="ColumnCount" /> is greater than 1: one string per column (left to right).</summary>
    public IReadOnlyList<string>? ColumnTexts { get; set; }
}