using Lyo.Pdf.Models;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Graphics;

namespace Lyo.Pdf;

/// <summary>Correlates stroked horizontal paths with word bounding boxes to detect vector underlines.</summary>
internal static class PdfUnderlineDetection
{
    /// <summary>Marks <see cref="PdfWordFormat.FontUnderline" /> when a candidate stroke intersects a band just under the word.</summary>
    public static IReadOnlyList<PdfWord> ApplyUnderlines(IReadOnlyList<PdfWord> words, IReadOnlyList<PdfPath> paths)
    {
        if (words.Count == 0 || paths.Count == 0)
            return words;

        var candidates = new List<PdfPath>();
        foreach (var path in paths) {
            if (!path.IsStroked || path.IsClipping)
                continue;

            var br = path.GetBoundingRectangle();
            if (!br.HasValue)
                continue;

            var r = br.Value;
            if (r.Width < 4 || r.Height > 8 || r.Width < r.Height * 1.5)
                continue;

            candidates.Add(path);
        }

        if (candidates.Count == 0)
            return words;

        var result = new List<PdfWord>(words.Count);
        foreach (var w in words)
            result.Add(WordHasUnderline(w, candidates) ? WithUnderline(w) : w);

        return result;
    }

    private static PdfWord WithUnderline(PdfWord w)
    {
        var f = w.Format;
        if (f == null)
            return w with { Format = new(FontUnderline: true) };

        if (f.FontUnderline)
            return w;

        return w with { Format = f with { FontUnderline = true } };
    }

    private static bool WordHasUnderline(PdfWord w, List<PdfPath> strokePaths)
    {
        var b = w.BoundingBox;
        var band = new PdfRectangle(b.Left - 1, b.Bottom - 14, b.Right + 1, b.Bottom + 3);
        return strokePaths.Any(path => path.IntersectsWith(band));
    }
}
