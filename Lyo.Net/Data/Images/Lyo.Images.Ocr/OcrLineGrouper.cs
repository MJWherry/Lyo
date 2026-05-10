using Lyo.Common.Records;
using Lyo.Images.Ocr.Models;

namespace Lyo.Images.Ocr;

/// <summary>Groups word boxes into lines using a simple Y-overlap heuristic.</summary>
public static class OcrLineGrouper
{
    /// <summary>Groups words into lines sorted top-to-bottom (decreasing Y-up).</summary>
    public static IReadOnlyList<OcrLine> GroupIntoLines(IReadOnlyList<OcrWord> words, double yMergeTolerancePixels)
    {
        ArgumentNullException.ThrowIfNull(words);
        if (words.Count == 0)
            return [];

        var sorted = words.OrderByDescending(w => w.BoundingBoxPixels.Top).ThenBy(w => w.BoundingBoxPixels.Left).ToList();
        var lines = new List<List<OcrWord>>();
        List<OcrWord>? current = null;
        double? lineMidY = null;

        foreach (var w in sorted) {
            var midY = (w.BoundingBoxPixels.Top + w.BoundingBoxPixels.Bottom) / 2;
            if (current == null) {
                current = [w];
                lineMidY = midY;
                continue;
            }

            if (lineMidY is { } my && Math.Abs(midY - my) <= yMergeTolerancePixels) {
                current.Add(w);
                lineMidY = current.Average(x => (x.BoundingBoxPixels.Top + x.BoundingBoxPixels.Bottom) / 2);
            }
            else {
                lines.Add(current);
                current = [w];
                lineMidY = midY;
            }
        }

        if (current != null)
            lines.Add(current);

        var result = new List<OcrLine>(lines.Count);
        foreach (var group in lines) {
            var ordered = group.OrderBy(w => w.BoundingBoxPixels.Left).ToList();
            var text = string.Join(" ", ordered.Select(x => x.Text).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();
            var union = UnionBoxes(ordered.Select(w => w.BoundingBoxPixels));
            result.Add(new OcrLine(text, union, ordered));
        }

        return result.OrderByDescending(l => l.BoundingBoxPixels.Top).ToList();
    }

    private static BoundingBox2D UnionBoxes(IEnumerable<BoundingBox2D> boxes)
    {
        var first = true;
        double left = 0, right = 0, top = 0, bottom = 0;
        foreach (var b in boxes) {
            if (first) {
                left = b.Left;
                right = b.Right;
                top = b.Top;
                bottom = b.Bottom;
                first = false;
            }
            else {
                left = Math.Min(left, b.Left);
                right = Math.Max(right, b.Right);
                top = Math.Max(top, b.Top);
                bottom = Math.Min(bottom, b.Bottom);
            }
        }

        return new(left, right, top, bottom);
    }
}
