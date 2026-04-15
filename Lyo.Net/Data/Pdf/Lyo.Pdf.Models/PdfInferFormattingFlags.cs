namespace Lyo.Pdf.Models;

/// <summary>How to infer key/value pairs or table headers from PDF text when keys or headers are omitted.</summary>
[Flags]
public enum PdfInferFormattingFlags
{
    None = 0,

    /// <summary>Use emphasized font (bold/italic via embedded font names and style flags).</summary>
    Bold = 1 << 0,

    /// <summary>Use punctuation-terminated labels: colon (<c>Key:</c>, <c>Key: value</c>) and semicolon (<c>Key;</c>, <c>Key; value</c>).</summary>
    Semicolon = 1 << 1,

    /// <summary>Use vector underlines (horizontal strokes under glyph bands) as emphasis.</summary>
    Underline = 1 << 2,
}
