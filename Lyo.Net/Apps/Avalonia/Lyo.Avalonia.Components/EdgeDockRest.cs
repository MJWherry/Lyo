namespace Lyo.Desktop.Components;

/// <summary>Size of the window after it leaves the screen edge. Height 0 with <paramref name="HeightToContent" /> leaves the height to the content.</summary>
/// <param name="Width">Width in device-independent pixels.</param>
/// <param name="Height">Height in device-independent pixels. Ignored when <paramref name="HeightToContent" /> is true.</param>
/// <param name="HeightToContent">True when the open window sizes its height from its content.</param>
public readonly record struct EdgeDockRest(double Width, double Height, bool HeightToContent)
{
    /// <summary>Screen pixels for a native move. A non-positive <paramref name="scale" /> is 1. Height is 0 when the content owns it, so the placer skips that axis.</summary>
    public (int Width, int Height) Pixels(double scale)
    {
        var factor = scale <= 0 ? 1d : scale;
        var width = Math.Max(1, (int)Math.Round(Width * factor));
        if (HeightToContent || Height <= 0)
            return (width, 0);

        return (width, Math.Max(1, (int)Math.Round(Height * factor)));
    }
}