namespace Lyo.Pdf.Models;

/// <summary>How values are positioned relative to known keys during key/value extraction.</summary>
public enum PdfKeyValueLayout
{
    /// <summary>Value text is to the right of the key on the same line (and may continue on subsequent lines in the same column).</summary>
    Horizontal = 0,

    /// <summary>Value text is below the key on subsequent lines (common for label-over-field forms).</summary>
    Vertical = 1
}
