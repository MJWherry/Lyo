using System.Diagnostics;

namespace Lyo.Desktop.Components;

/// <summary>Which side of the work area a window bubble is parked on. The flat side faces this edge.</summary>
public enum ScreenEdge
{
    /// <summary>Flat side on the left of the work area.</summary>
    Left,

    /// <summary>Flat side on the right of the work area.</summary>
    Right,

    /// <summary>Flat side on the top of the work area.</summary>
    Top,

    /// <summary>Flat side on the bottom of the work area.</summary>
    Bottom
}

/// <summary>Pixel position and size of a docked bubble.</summary>
/// <param name="Edge">Side the flat edge sits on.</param>
/// <param name="X">Left edge of the bubble, in screen pixels.</param>
/// <param name="Y">Top edge of the bubble, in screen pixels.</param>
/// <param name="Width">Bubble width, in screen pixels.</param>
/// <param name="Height">Bubble height, in screen pixels.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ScreenEdgeAnchor(ScreenEdge Edge, int X, int Y, int Width, int Height);

/// <summary>Corner radii for a bubble whose flat side sits on a screen edge. Values are in device-independent pixels.</summary>
/// <param name="TopLeft">Top-left radius.</param>
/// <param name="TopRight">Top-right radius.</param>
/// <param name="BottomRight">Bottom-right radius.</param>
/// <param name="BottomLeft">Bottom-left radius.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ScreenEdgeCorners(int TopLeft, int TopRight, int BottomRight, int BottomLeft);

/// <summary>Picks the nearest screen edge for a docked window bubble.</summary>
public static class ScreenEdgeDock
{
    /// <summary>Thickness of the bubble in device-independent pixels. Callers scale this to screen pixels before <see cref="Place" />.</summary>
    public const int Thickness = 28;

    /// <summary>Length of the bubble in device-independent pixels. Twice <see cref="Thickness" />, so the rounded side is a semicircle.</summary>
    public const int Diameter = 56;

    /// <summary>How far the pointer must move inward from the docked edge, in screen pixels, before a drag leaves the edge.</summary>
    public const int OffEdge = 72;

    /// <summary>Rounds the side that faces away from <paramref name="edge" />. The flat side stays square.</summary>
    public static ScreenEdgeCorners Corners(ScreenEdge edge) => Corners(edge, Thickness);

    /// <summary>Rounds the outer side by <paramref name="radius" />. The side on the screen edge stays square.</summary>
    public static ScreenEdgeCorners Corners(ScreenEdge edge, int radius)
    {
        if (radius < 0)
            radius = 0;

        return edge switch {
            ScreenEdge.Left => new(0, radius, radius, 0),
            ScreenEdge.Right => new(radius, 0, 0, radius),
            ScreenEdge.Top => new(0, 0, radius, radius),
            var _ => new(radius, radius, 0, 0)
        };
    }

    /// <summary>
    /// Parks a bubble on the nearest edge of a work area. The window center decides the edge. The bubble is centered on the window along that edge, then clamped inside the work
    /// area.
    /// </summary>
    public static ScreenEdgeAnchor Place(
        int screenX,
        int screenY,
        int screenWidth,
        int screenHeight,
        int windowX,
        int windowY,
        int windowWidth,
        int windowHeight,
        int thickness,
        int diameter)
    {
        var centerX = windowX + windowWidth / 2;
        var centerY = windowY + windowHeight / 2;
        var left = centerX - screenX;
        var right = screenX + screenWidth - centerX;
        var top = centerY - screenY;
        var bottom = screenY + screenHeight - centerY;
        var edge = ScreenEdge.Left;
        var nearest = left;
        if (right < nearest) {
            nearest = right;
            edge = ScreenEdge.Right;
        }

        if (top < nearest) {
            nearest = top;
            edge = ScreenEdge.Top;
        }

        if (bottom < nearest)
            edge = ScreenEdge.Bottom;

        var vertical = edge is ScreenEdge.Left or ScreenEdge.Right;
        var along = vertical ? centerY - diameter / 2 : centerX - diameter / 2;
        return Along(edge, screenX, screenY, screenWidth, screenHeight, along, thickness, diameter);
    }

    /// <summary>
    /// Parks a bubble on <paramref name="edge" />. <paramref name="along" /> is the top for a side edge and the left for the top or bottom. It is clamped so the bubble stays
    /// inside the work area.
    /// </summary>
    public static ScreenEdgeAnchor Along(ScreenEdge edge, int screenX, int screenY, int screenWidth, int screenHeight, int along, int thickness, int diameter)
    {
        var vertical = edge is ScreenEdge.Left or ScreenEdge.Right;
        var origin = vertical ? screenY : screenX;
        var limit = vertical ? screenHeight : screenWidth;
        var max = origin + limit - diameter;
        if (max < origin)
            max = origin;

        if (along < origin)
            along = origin;

        if (along > max)
            along = max;

        return At(edge, screenX, screenY, screenWidth, screenHeight, along, thickness, diameter);
    }

    /// <summary>
    /// Sizes <paramref name="collapsed" /> around content. The side on the screen edge stays put. Width and height are screen pixels and may be larger than the collapsed face,
    /// so a top or bottom tab can show a row of buttons.
    /// </summary>
    public static ScreenEdgeAnchor Fit(ScreenEdgeAnchor collapsed, int contentWidth, int contentHeight)
    {
        var inward = collapsed.Edge is ScreenEdge.Left or ScreenEdge.Right ? contentWidth - collapsed.Width : contentHeight - collapsed.Height;
        var fitted = Expand(collapsed, inward);
        if (collapsed.Edge is ScreenEdge.Left or ScreenEdge.Right)
            return fitted with { Height = Math.Max(fitted.Height, contentHeight) };

        return fitted with { Width = Math.Max(fitted.Width, contentWidth) };
    }

    /// <summary>
    /// Pulls <paramref name="anchor" /> back into the work area. The side that sits on the screen edge stays on that edge. A tab that is too wide or too tall slides along the
    /// edge instead of hanging off the screen.
    /// </summary>
    public static ScreenEdgeAnchor Clamp(ScreenEdgeAnchor anchor, int screenX, int screenY, int screenWidth, int screenHeight)
    {
        var width = anchor.Width;
        var height = anchor.Height;
        if (width > screenWidth)
            width = Math.Max(1, screenWidth);

        if (height > screenHeight)
            height = Math.Max(1, screenHeight);

        var right = screenX + screenWidth;
        var bottom = screenY + screenHeight;
        var x = anchor.Edge switch {
            ScreenEdge.Left => screenX,
            ScreenEdge.Right => right - width,
            var _ => anchor.X
        };

        var y = anchor.Edge switch {
            ScreenEdge.Top => screenY,
            ScreenEdge.Bottom => bottom - height,
            var _ => anchor.Y
        };

        if (anchor.Edge is ScreenEdge.Left or ScreenEdge.Right) {
            if (y < screenY)
                y = screenY;

            if (y + height > bottom)
                y = bottom - height;
        }
        else {
            if (x < screenX)
                x = screenX;

            if (x + width > right)
                x = right - width;
        }

        return anchor with {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }

    /// <summary>
    /// How far to shift the icon from the center of the bubble, in the same units as <paramref name="max" />. Positive moves toward the end of the edge (right or bottom). Zero
    /// keeps the icon centered. <paramref name="slackStart" /> and <paramref name="slackEnd" /> are the free pixels before each end of the work area. The shift eases in over
    /// <paramref name="travel" /> and stops at <paramref name="max" />, which is half the difference between the bubble's length and its thickness. A bubble tucked into a corner then
    /// turns onto the next edge without the icon jumping outward.
    /// </summary>
    public static double IconBias(int slackStart, int slackEnd, double travel, double max)
    {
        if (max <= 0 || travel <= 0)
            return 0;

        return Toward(slackEnd, travel, max) - Toward(slackStart, travel, max);

        static double Toward(int slack, double travel, double max)
        {
            if (slack <= 0)
                return max;

            if (slack >= travel)
                return 0;

            return max * (1 - slack / travel);
        }
    }

    /// <summary>Grows <paramref name="anchor" /> inward by <paramref name="extra" /> screen pixels. The screen-edge side stays put.</summary>
    public static ScreenEdgeAnchor Expand(ScreenEdgeAnchor anchor, int extra)
    {
        if (extra <= 0)
            return anchor;

        return anchor.Edge switch {
            ScreenEdge.Left => anchor with { Width = anchor.Width + extra },
            ScreenEdge.Right => anchor with { X = anchor.X - extra, Width = anchor.Width + extra },
            ScreenEdge.Top => anchor with { Height = anchor.Height + extra },
            var _ => anchor with { Y = anchor.Y - extra, Height = anchor.Height + extra }
        };
    }

    /// <summary>True when <paramref name="pointerX" /> / <paramref name="pointerY" /> has moved inward from <paramref name="edge" /> by more than <paramref name="threshold" />.</summary>
    public static bool PulledOff(ScreenEdge edge, int screenX, int screenY, int screenWidth, int screenHeight, int pointerX, int pointerY, int threshold)
    {
        var inward = edge switch {
            ScreenEdge.Left => pointerX - screenX,
            ScreenEdge.Right => screenX + screenWidth - pointerX,
            ScreenEdge.Top => pointerY - screenY,
            var _ => screenY + screenHeight - pointerY
        };

        return inward > threshold;
    }

    /// <summary>True when the pointer is farther than <paramref name="threshold" /> from every side of the work area.</summary>
    public static bool AwayFromEdges(int screenX, int screenY, int screenWidth, int screenHeight, int pointerX, int pointerY, int threshold)
    {
        var left = pointerX - screenX;
        var right = screenX + screenWidth - pointerX;
        var top = pointerY - screenY;
        var bottom = screenY + screenHeight - pointerY;
        var nearest = left;
        if (right < nearest)
            nearest = right;

        if (top < nearest)
            nearest = top;

        if (bottom < nearest)
            nearest = bottom;

        return nearest > threshold;
    }

    private static ScreenEdgeAnchor At(ScreenEdge edge, int screenX, int screenY, int screenWidth, int screenHeight, int along, int thickness, int diameter)
    {
        var vertical = edge is ScreenEdge.Left or ScreenEdge.Right;
        var x = edge switch {
            ScreenEdge.Left => screenX,
            ScreenEdge.Right => screenX + screenWidth - thickness,
            var _ => along
        };

        var y = edge switch {
            ScreenEdge.Top => screenY,
            ScreenEdge.Bottom => screenY + screenHeight - thickness,
            var _ => along
        };

        return new(edge, x, y, vertical ? thickness : diameter, vertical ? diameter : thickness);
    }
}