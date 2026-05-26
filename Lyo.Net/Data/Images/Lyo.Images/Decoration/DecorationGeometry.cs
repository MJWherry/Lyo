using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace Lyo.Images.Decoration;

/// <summary>Shared geometry helpers (rounded rect / top-rounded rect paths, hex color parsing) used by the decoration primitives.</summary>
internal static class DecorationGeometry
{
    /// <summary>Bézier handle factor for approximating a quarter-circle via cubic curves (stable with ImageSharp.Drawing 2.x; avoids degenerate ArcTo output).</summary>
    private const float QuarterCircleBezierK = 0.5522847498f;

    /// <summary>Closed rounded rectangle using cubic Bézier quarter-circle approximations.</summary>
    public static IPath RoundedRectPath(float x, float y, float w, float h, float radius)
    {
        if (w <= 0 || h <= 0)
            return new RectangularPolygon(x, y, Math.Max(0, w), Math.Max(0, h));

        var rr = Math.Min(radius, Math.Min(w, h) / 2f);
        if (rr <= 0.5f)
            return new RectangularPolygon(x, y, w, h);

        var k = QuarterCircleBezierK;
        var pb = new PathBuilder();
        pb.MoveTo(new(x + rr, y));
        pb.LineTo(new(x + w - rr, y));
        pb.CubicBezierTo(new(x + w - rr + k * rr, y), new(x + w, y + rr - k * rr), new(x + w, y + rr));
        pb.LineTo(new(x + w, y + h - rr));
        pb.CubicBezierTo(new(x + w, y + h - rr + k * rr), new(x + w - rr + k * rr, y + h), new(x + w - rr, y + h));
        pb.LineTo(new(x + rr, y + h));
        pb.CubicBezierTo(new(x + rr - k * rr, y + h), new(x, y + h - rr + k * rr), new(x, y + h - rr));
        pb.LineTo(new(x, y + rr));
        pb.CubicBezierTo(new(x, y + rr - k * rr), new(x + rr - k * rr, y), new(x + rr, y));
        pb.CloseFigure();
        return pb.Build();
    }

    /// <summary>Rounded top corners only; bottom edge square (caption band header).</summary>
    public static IPath TopRoundedRectPath(float x, float y, float w, float h, float radius)
    {
        if (w <= 0 || h <= 0)
            return new RectangularPolygon(x, y, Math.Max(0, w), Math.Max(0, h));

        var rr = Math.Min(radius, Math.Min(w, h) / 2f);
        if (rr <= 0.5f)
            return new RectangularPolygon(x, y, w, h);

        var k = QuarterCircleBezierK;
        var pb = new PathBuilder();
        pb.MoveTo(new(x + rr, y));
        pb.LineTo(new(x + w - rr, y));
        pb.CubicBezierTo(new(x + w - rr + k * rr, y), new(x + w, y + rr - k * rr), new(x + w, y + rr));
        pb.LineTo(new(x + w, y + h));
        pb.LineTo(new(x, y + h));
        pb.LineTo(new(x, y + rr));
        pb.CubicBezierTo(new(x, y + rr - k * rr), new(x + rr - k * rr, y), new(x + rr, y));
        pb.CloseFigure();
        return pb.Build();
    }

    /// <summary>Rounded bottom corners only; top edge square (caption band footer).</summary>
    public static IPath BottomRoundedRectPath(float x, float y, float w, float h, float radius)
    {
        if (w <= 0 || h <= 0)
            return new RectangularPolygon(x, y, Math.Max(0, w), Math.Max(0, h));

        var rr = Math.Min(radius, Math.Min(w, h) / 2f);
        if (rr <= 0.5f)
            return new RectangularPolygon(x, y, w, h);

        var k = QuarterCircleBezierK;
        var pb = new PathBuilder();
        pb.MoveTo(new(x, y));
        pb.LineTo(new(x + w, y));
        pb.LineTo(new(x + w, y + h - rr));
        pb.CubicBezierTo(new(x + w, y + h - rr + k * rr), new(x + w - rr + k * rr, y + h), new(x + w - rr, y + h));
        pb.LineTo(new(x + rr, y + h));
        pb.CubicBezierTo(new(x + rr - k * rr, y + h), new(x, y + h - rr + k * rr), new(x, y + h - rr));
        pb.LineTo(new(x, y));
        pb.CloseFigure();
        return pb.Build();
    }

    /// <summary>Tries to parse a hex color, retrying without the alpha channel when MudBlazor-style <c>#RRGGBBAA</c> values are supplied.</summary>
    public static bool TryParseColor(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        var h = hex.Trim();
        if (Color.TryParse(h, out color))
            return true;

        if (h.Length == 9 && h[0] == '#' && Color.TryParse(h[..7], out color))
            return true;

        return false;
    }

    /// <summary>Parses <paramref name="hex" />, falling back to <paramref name="fallback" /> on null/invalid input.</summary>
    public static Color ParseColorOr(string? hex, Color fallback)
        => TryParseColor(hex, out var c) ? c : fallback;
}
