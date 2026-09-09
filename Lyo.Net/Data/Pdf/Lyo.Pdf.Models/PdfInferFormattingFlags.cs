namespace Lyo.Pdf.Models;

/// <summary>Cues used to infer keys or table headers when they are not supplied.</summary>
[Flags]
public enum PdfInferFormattingFlags
{
    None = 0,

    /// <summary>Treat bold/italic (embedded font names and style flags) as emphasis.</summary>
    Bold = 1 << 0,

    /// <summary>Treat colon/semicolon-terminated labels as keys (<c>Key:</c>, <c>Key: value</c>, <c>Key;</c>, <c>Key; value</c>).</summary>
    Semicolon = 1 << 1,

    /// <summary>Treat horizontal strokes under glyph bands as emphasis.</summary>
    Underline = 1 << 2
}