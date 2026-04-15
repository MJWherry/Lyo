using Lyo.Pdf.Models;

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

    /// <summary>When extraction is <see cref="PdfAnnotationExtractionType.KeyValue" />, whether values are to the right of keys or on lines below.</summary>
    public PdfKeyValueLayout KeyValueLayout { get; set; } = PdfKeyValueLayout.Horizontal;

    /// <summary>When inferring key/value or table headers without chips, which signals to use (bold, underline, optional punctuation-terminated labels).</summary>
    public PdfInferFormattingFlags InferFormattingFlags { get; set; } = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline;

    /// <summary>
    /// When <see cref="InferFormattingFlags" /> includes <see cref="PdfInferFormattingFlags.Semicolon" />, key/value and table-header inference tries these punctuation characters as label terminators, in order (e.g. <c>":;=?|"</c>).
    /// </summary>
    public string KeyValueInferDelimiters { get; set; } = ":;";

    /// <summary>
    /// When extraction is <see cref="PdfAnnotationExtractionType.Table" />, optional header label for the row-key column. When set, only this column is treated as
    /// <see cref="ColumnHeader.IsKey" /> (continuation lines without this cell merge into the previous row). Overrides <c>*</c> prefix on chips.
    /// </summary>
    public string? TableKeyColumnLabel { get; set; }

    /// <summary>
    /// When extraction is <see cref="PdfAnnotationExtractionType.BoundingBoxText" />, splits the region into this many vertical columns (≥ 1). Values &gt; 1 use gutter/band
    /// detection from the PDF layout.
    /// </summary>
    public int ColumnCount { get; set; } = 1;

    /// <summary>Populated when <see cref="ColumnCount" /> is greater than 1: one string per column (left to right).</summary>
    public IReadOnlyList<string>? ColumnTexts { get; set; }
}