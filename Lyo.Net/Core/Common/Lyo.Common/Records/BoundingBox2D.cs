using System.Diagnostics;

namespace Lyo.Common.Records;

/// <summary>Axis-aligned 2D bounding box defined by Left, Right, Top, and Bottom edges.</summary>
/// <param name="Left">Left edge X.</param>
/// <param name="Right">Right edge X.</param>
/// <param name="Top">Top edge Y (typically larger values = higher in coordinate system).</param>
/// <param name="Bottom">Bottom edge Y.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct BoundingBox2D(double Left, double Right, double Top, double Bottom)
{
    /// <summary>Width of the bounding box (Right - Left).</summary>
    public double Width => Right - Left;

    /// <summary>Height of the bounding box (Top - Bottom).</summary>
    public double Height => Top - Bottom;

    /// <summary>Checks if a point is within this bounding box.</summary>
    public bool Contains(double x, double y) => x >= Left && x <= Right && y >= Bottom && y <= Top;

    /// <summary>Checks if this bounding box intersects with another.</summary>
    public bool Intersects(BoundingBox2D other) => Left < other.Right && Right > other.Left && Bottom < other.Top && Top > other.Bottom;

    /// <summary>Returns the fraction of this box's area that overlaps with the other box. 0 if no overlap. Used for overlap-threshold filtering.</summary>
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