using System.Diagnostics;

namespace Lyo.Common.Metadata.Records;

/// <summary>Axis-aligned 2D box whose edges are <paramref name="Left" />, <paramref name="Right" />, <paramref name="Top" />, and <paramref name="Bottom" />.</summary>
/// <param name="Left">Left-edge X.</param>
/// <param name="Right">Right-edge X.</param>
/// <param name="Top">Top-edge Y (larger values usually sit higher in the coordinate system).</param>
/// <param name="Bottom">Bottom-edge Y.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BoundingBox2D(double Left, double Right, double Top, double Bottom)
{
    /// <summary>Box width (<c>Right - Left</c>).</summary>
    public double Width => Right - Left;

    /// <summary>Box height (<c>Top - Bottom</c>).</summary>
    public double Height => Top - Bottom;

    /// <summary>True when the point sits inside or on the edge of this box.</summary>
    public bool Contains(double x, double y) => x >= Left && x <= Right && y >= Bottom && y <= Top;

    /// <summary>True when this box overlaps <paramref name="other" />.</summary>
    public bool Intersects(BoundingBox2D other) => Left < other.Right && Right > other.Left && Bottom < other.Top && Top > other.Bottom;

    /// <summary>Overlap area divided by this box's area; 0 when the boxes do not overlap. Intended for overlap-threshold filters.</summary>
    public double OverlapRatio(BoundingBox2D other)
    {
        var interLeft = Math.Max(Left, other.Left);
        var interRight = Math.Min(Right, other.Right);
        var interTop = Math.Min(Top, other.Top);
        var interBottom = Math.Max(Bottom, other.Bottom);
        if (interLeft >= interRight || interBottom >= interTop)
            return 0;

        var interArea = (interRight - interLeft) * (interTop - interBottom);
        var thisArea = Width * Height;
        return thisArea > 0 ? interArea / thisArea : 0;
    }

    public override string ToString() => $"Left: {Left}, Right: {Right}, Top: {Top}, Bottom: {Bottom}";
}