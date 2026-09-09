namespace Lyo.Pdf.Models;

/// <summary>Style metadata attached to a PDF word (font, color, emphasis).</summary>
/// <param name="FontSize">Point size when the PDF exposes it.</param>
/// <param name="FontName">Family name when the PDF exposes it.</param>
/// <param name="FontBold">True when the font is bold.</param>
/// <param name="FontItalic">True when the font is italic.</param>
/// <param name="FontColor">Hex color (e.g. #FF0000) when the PDF exposes it.</param>
/// <param name="FontUnderline">True when a stroked path crosses the word band (vector underline detection).</param>
public sealed record PdfWordFormat(
    double? FontSize = null,
    string? FontName = null,
    bool FontBold = false,
    bool FontItalic = false,
    string? FontColor = null,
    bool FontUnderline = false);