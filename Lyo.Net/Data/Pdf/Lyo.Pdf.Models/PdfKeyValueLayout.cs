namespace Lyo.Pdf.Models;

/// <summary>Where extracted values sit relative to known keys.</summary>
public enum PdfKeyValueLayout
{
    /// <summary>Value sits to the right of the key on the same line (and may wrap onto later lines in that column).</summary>
    Horizontal = 0,

    /// <summary>Value sits below the key on later lines (typical label-over-field forms).</summary>
    Vertical = 1
}