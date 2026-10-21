namespace Lyo.Desktop.Components;

/// <summary>Where a window sits when it is docked to a screen edge, and where to put it back.</summary>
public sealed class EdgeDockPlacement
{
    /// <summary>True when the window is the edge bubble.</summary>
    public bool Docked { get; set; }

    /// <summary>Side the flat edge sits on.</summary>
    public ScreenEdge Edge { get; set; } = ScreenEdge.Right;

    /// <summary>Left of the collapsed bubble, in screen pixels.</summary>
    public int X { get; set; }

    /// <summary>Top of the collapsed bubble, in screen pixels.</summary>
    public int Y { get; set; }

    /// <summary>Window X before it was docked, in screen pixels.</summary>
    public int RestX { get; set; }

    /// <summary>Window Y before it was docked, in screen pixels.</summary>
    public int RestY { get; set; }

    /// <summary>Top for a side edge, left for the top or bottom.</summary>
    public int Along => Edge is ScreenEdge.Left or ScreenEdge.Right ? Y : X;

    /// <summary>Copies a saved record. <paramref name="edge" /> outside 0–3 clamps to a real <see cref="ScreenEdge" />.</summary>
    public void Apply(bool docked, int edge, int x, int y, int restX, int restY)
    {
        Docked = docked;
        Edge = (ScreenEdge)Math.Clamp(edge, 0, 3);
        X = x;
        Y = y;
        RestX = restX;
        RestY = restY;
    }

    /// <summary>The same fields <see cref="Apply" /> reads, with the edge as an int.</summary>
    public (bool Docked, int Edge, int X, int Y, int RestX, int RestY) Values() => (Docked, (int)Edge, X, Y, RestX, RestY);
}