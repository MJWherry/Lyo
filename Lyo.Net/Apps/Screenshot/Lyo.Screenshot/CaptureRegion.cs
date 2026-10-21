namespace Lyo.Screenshot;

/// <summary>Screen rectangle in pixels. Width and height are the viewport that is grabbed on each frame.</summary>
public readonly record struct CaptureRegion(int X, int Y, int Width, int Height)
{
    /// <summary>Intersection of this rectangle with <paramref name="screen" />. No overlap returns a zero width and height.</summary>
    public CaptureRegion ClippedTo(CaptureRegion screen)
    {
        var left = Math.Max(X, screen.X);
        var top = Math.Max(Y, screen.Y);
        var right = Math.Min((long)X + Width, (long)screen.X + screen.Width);
        var bottom = Math.Min((long)Y + Height, (long)screen.Y + screen.Height);
        if (right <= left || bottom <= top)
            return new(left, top, 0, 0);

        return new(left, top, (int)(right - left), (int)(bottom - top));
    }
}