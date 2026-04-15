using Lyo.Common.Records;
using Lyo.Pdf.Models;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;

namespace Lyo.Pdf;

/// <summary>Maps PdfPig Word to Lyo PdfWord.</summary>
internal static class PdfWordMapping
{
    public static PdfWord ToPdfWord(this Word w)
    {
        var bbox = new BoundingBox2D(w.BoundingBox.Left, w.BoundingBox.Right, w.BoundingBox.Top, w.BoundingBox.Bottom);
        var format = ExtractFormat(w);
        return new(w.Text, bbox, format);
    }

    public static IReadOnlyList<PdfWord> ToPdfWords(this IEnumerable<Word> words) => words.Select(ToPdfWord).ToList();

    public static IReadOnlyList<PdfWord> ToPdfWords(this IEnumerable<Word> words, IReadOnlyList<PdfPath> pagePaths)
    {
        var list = words.Select(ToPdfWord).ToList();
        if (pagePaths == null || pagePaths.Count == 0)
            return list;

        return PdfUnderlineDetection.ApplyUnderlines(list, pagePaths);
    }

    private static PdfWordFormat? ExtractFormat(Word w)
    {
        if (w.Letters.Count == 0)
            return null;

        var letter = w.Letters[0];
        var fd = letter.FontDetails;
        var fontSize = letter.PointSize > 0 ? letter.PointSize : (double?)null;
        var fontName = !string.IsNullOrEmpty(letter.FontName) ? letter.FontName : null;
        var fontColor = TryColorToHex(letter.Color);
        var bold = PdfFontStyleInference.InferBold(fontName, fd.IsBold);
        var italic = PdfFontStyleInference.InferItalic(fontName, fd.IsItalic);
        return new(fontSize, fontName, bold, italic, fontColor);
    }

    private static string? TryColorToHex(IColor color)
    {
        if (color == null)
            return null;

        try {
            var (r, g, b) = color.ToRGBValues();
            var rr = (byte)(Math.Max(0, Math.Min(1, r)) * 255);
            var gg = (byte)(Math.Max(0, Math.Min(1, g)) * 255);
            var bb = (byte)(Math.Max(0, Math.Min(1, b)) * 255);
            return $"#{rr:X2}{gg:X2}{bb:X2}";
        }
        catch {
            return null;
        }
    }
}