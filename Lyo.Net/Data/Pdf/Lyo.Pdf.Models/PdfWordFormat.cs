namespace Lyo.Pdf.Models;

/// <summary>Formatting metadata for a PDF word (font, color, style).</summary>
/// <param name="FontSize">Font size in points, if available.</param>
/// <param name="FontName">Font family name, if available.</param>
/// <param name="FontBold">Whether the font is bold.</param>
/// <param name="FontItalic">Whether the font is italic.</param>
/// <param name="FontColor">Font color as hex (e.g. #FF0000), if available.</param>
/// <param name="FontUnderline">Whether a stroked path intersects the word band (detected from vector underlines).</param>
public sealed record PdfWordFormat(
    double? FontSize = null,
    string? FontName = null,
    bool FontBold = false,
    bool FontItalic = false,
    string? FontColor = null,
    bool FontUnderline = false);