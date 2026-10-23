using Lyo.Exceptions;

namespace Lyo.Desktop.Components;

/// <summary>Edge a tool pane can dock to. The center control stays put.</summary>
public enum ToolDockEdge
{
    /// <summary>Left of the center.</summary>
    Left,

    /// <summary>Right of the center.</summary>
    Right,

    /// <summary>Under the center.</summary>
    Bottom
}

/// <summary>One docked tool pane. The center is not a pane.</summary>
public sealed class ToolDockPane
{
    /// <summary>Stable id. The host maps this back to its own model.</summary>
    public required string Id { get; init; }

    /// <summary>Title shown on the pane header.</summary>
    public required string Title { get; init; }

    /// <summary>Edge it occupies.</summary>
    public ToolDockEdge Edge { get; set; }

    /// <summary>False when the user has hidden it.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>True when the body is hidden and only the header shows.</summary>
    public bool Minimized { get; set; }
}

/// <summary>Where tool panes sit around a center control.</summary>
public sealed class ToolDockState
{
    /// <summary>Width of the left edge, in pixels.</summary>
    public double Left { get; set; } = 280;

    /// <summary>Width of the right edge, in pixels.</summary>
    public double Right { get; set; } = 280;

    /// <summary>Height of the bottom edge, in pixels.</summary>
    public double Bottom { get; set; } = 160;

    /// <summary>Docked panes. Never the center control.</summary>
    public List<ToolDockPane> Panes { get; } = [];

    /// <summary>Builds a layout from id, title, edge, and visibility.</summary>
    public static ToolDockState Create(params (string Id, string Title, ToolDockEdge Edge, bool Visible)[] panes)
    {
        ArgumentHelpers.ThrowIfNull(panes);
        var state = new ToolDockState();
        foreach (var pane in panes)
            state.Panes.Add(
                new() {
                    Id = pane.Id,
                    Title = pane.Title,
                    Edge = pane.Edge,
                    Visible = pane.Visible
                });

        return state;
    }

    /// <summary>The pane for <paramref name="id" />.</summary>
    public ToolDockPane Pane(string id)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        return Panes.First(pane => pane.Id == id);
    }

    /// <summary>Edge of <paramref name="id" />.</summary>
    public ToolDockEdge Edge(string id) => Pane(id).Edge;

    /// <summary>Whether <paramref name="id" /> is shown.</summary>
    public bool IsVisible(string id) => Pane(id).Visible;

    /// <summary>Moves <paramref name="id" /> onto <paramref name="edge" /> and shows it.</summary>
    public void Move(string id, ToolDockEdge edge)
    {
        var pane = Pane(id);
        pane.Edge = edge;
        pane.Visible = true;
    }

    /// <summary>Shows or hides <paramref name="id" /> without changing its edge.</summary>
    public void SetVisible(string id, bool visible) => Pane(id).Visible = visible;

    /// <summary>Collapses <paramref name="id" /> to its header, or opens it again.</summary>
    public void ToggleMinimized(string id)
    {
        var pane = Pane(id);
        pane.Minimized = !pane.Minimized;
    }

    /// <summary>True when every visible pane on <paramref name="edge" /> is minimized, so the edge should shrink to the headers.</summary>
    public bool HugsHeader(ToolDockEdge edge)
    {
        var shown = false;
        foreach (var pane in Panes) {
            if (!pane.Visible || pane.Edge != edge)
                continue;

            if (!pane.Minimized)
                return false;

            shown = true;
        }

        return shown;
    }
}

/// <summary>Which edge a pointer is in while a pane header is dragged. The bottom band wins in a corner.</summary>
public static class ToolDockHit
{
    /// <summary>Width of the drop band, in pixels.</summary>
    public const double Band = 56;

    /// <summary>Edge under <paramref name="x" /> and <paramref name="y" />, or null when the pointer is in the center.</summary>
    public static ToolDockEdge? EdgeAt(double x, double y, double width, double height)
    {
        if (y >= height - Band)
            return ToolDockEdge.Bottom;

        if (x <= Band)
            return ToolDockEdge.Left;

        if (x >= width - Band)
            return ToolDockEdge.Right;

        return null;
    }
}