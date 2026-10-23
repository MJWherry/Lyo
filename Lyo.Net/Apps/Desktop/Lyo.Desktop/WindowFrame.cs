namespace Lyo.Desktop;

/// <summary>Turns a client rectangle into the outer frame rectangle.</summary>
public static class WindowFrame
{
    /// <summary>
    /// Expands <paramref name="client" /> by the window-manager frame. <paramref name="left" />, <paramref name="right" />, <paramref name="top" />, and
    /// <paramref name="bottom" /> are pixels. Negative extents are treated as zero.
    /// </summary>
    public static DesktopRect Outer(DesktopRect client, int left, int right, int top, int bottom)
    {
        if (left < 0)
            left = 0;

        if (right < 0)
            right = 0;

        if (top < 0)
            top = 0;

        if (bottom < 0)
            bottom = 0;

        return new(client.X - left, client.Y - top, client.Width + left + right, client.Height + top + bottom);
    }
}