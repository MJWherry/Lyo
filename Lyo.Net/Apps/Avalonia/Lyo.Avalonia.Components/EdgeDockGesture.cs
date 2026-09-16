namespace Lyo.Desktop.Components;

/// <summary>What a press and release on an edge dock meant.</summary>
public enum EdgeDockTap
{
    /// <summary>No press was in progress.</summary>
    None,

    /// <summary>The pointer went down and up without leaving the slop box.</summary>
    Click,

    /// <summary>A second click landed on the first one inside the double-click window.</summary>
    DoubleClick,

    /// <summary>The pointer moved farther than the slop before it came up.</summary>
    Drag
}

/// <summary>Turns pointer samples into a click, a double-click, or a drag. Coordinates are screen pixels. Time is a millisecond clock.</summary>
public sealed class EdgeDockGesture
{
    private bool _down;
    private bool _moved;
    private int _originX;
    private int _originY;
    private int _lastClickMs = int.MinValue;
    private int _lastX;
    private int _lastY;

    /// <summary>Movement, in pixels, that turns a press into a drag.</summary>
    public int Slop { get; init; } = 4;

    /// <summary>Two clicks closer than this, in milliseconds, are a double-click.</summary>
    public int DoubleClickMs { get; init; } = 400;

    /// <summary>Starts a press at <paramref name="x" />, <paramref name="y" />.</summary>
    public void Press(int x, int y)
    {
        _down = true;
        _moved = false;
        _originX = x;
        _originY = y;
    }

    /// <summary>True once the pointer has left the slop box around the press.</summary>
    public bool Move(int x, int y)
    {
        if (!_down)
            return false;
        var dx = x - _originX;
        var dy = y - _originY;
        if ((dx * dx) + (dy * dy) >= Slop * Slop)
            _moved = true;
        return _moved;
    }

    /// <summary>Ends the press. A drag does not count as a click.</summary>
    public EdgeDockTap Release(int x, int y, int timeMs)
    {
        if (!_down)
            return EdgeDockTap.None;
        _down = false;
        if (_moved)
            return EdgeDockTap.Drag;
        var dx = x - _lastX;
        var dy = y - _lastY;
        var repeat = timeMs - _lastClickMs <= DoubleClickMs && (dx * dx) + (dy * dy) <= Slop * Slop;
        _lastClickMs = timeMs;
        _lastX = x;
        _lastY = y;
        return repeat ? EdgeDockTap.DoubleClick : EdgeDockTap.Click;
    }

    /// <summary>Drops a press that the consumer handled, so the later release is not a click.</summary>
    public void Cancel() => _down = false;
}
