namespace Lyo.Job.Web.Components;

/// <summary>
/// Pan and zoom for the workflow diagram. Screen position is <c>diagram * Zoom + pan</c>, in CSS pixels, with the origin at the viewport's top left.
/// </summary>
/// <remarks>
/// <see cref="ZoomAt" /> keeps the diagram point under the cursor fixed, which is what a scroll-to-zoom gesture has to do. <see cref="Pan" /> adds a pointer delta in screen pixels.
/// </remarks>
public sealed class JobWorkflowGraphCamera
{
    /// <summary>Closest zoom. Past this the labels stop being worth shrinking further.</summary>
    public const double MinZoom = 0.4;

    /// <summary>Furthest zoom.</summary>
    public const double MaxZoom = 3;

    /// <summary>Scale applied to diagram coordinates. 1 draws the layout at its pixel size.</summary>
    public double Zoom { get; private set; } = 1;

    /// <summary>Horizontal offset of the diagram origin, in viewport pixels.</summary>
    public double PanX { get; private set; }

    /// <summary>Vertical offset of the diagram origin, in viewport pixels.</summary>
    public double PanY { get; private set; }

    /// <summary>Moves the diagram by a pointer delta, in viewport pixels.</summary>
    public void Pan(double deltaX, double deltaY)
    {
        if (!double.IsFinite(deltaX) || !double.IsFinite(deltaY))
            return;

        PanX += deltaX;
        PanY += deltaY;
    }

    /// <summary>
    /// Multiplies <see cref="Zoom" /> by <paramref name="factor" />, clamped to <see cref="MinZoom" /> and <see cref="MaxZoom" />, keeping the diagram point under
    /// (<paramref name="originX" />, <paramref name="originY" />) on that same screen point. The origin is in viewport pixels.
    /// </summary>
    public void ZoomAt(double factor, double originX, double originY)
    {
        if (!double.IsFinite(factor) || factor <= 0 || !double.IsFinite(originX) || !double.IsFinite(originY) || Zoom == 0)
            return;

        var next = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        var diagramX = (originX - PanX) / Zoom;
        var diagramY = (originY - PanY) / Zoom;
        Zoom = next;
        PanX = originX - (diagramX * next);
        PanY = originY - (diagramY * next);
    }

    /// <summary>Puts the camera back to 1:1 with the diagram origin at (<paramref name="panX" />, <paramref name="panY" />).</summary>
    public void Reset(double panX, double panY)
    {
        Zoom = 1;
        PanX = panX;
        PanY = panY;
    }

    /// <summary>Where a diagram point lands in the viewport.</summary>
    public (double X, double Y) ToScreen(double diagramX, double diagramY) => ((diagramX * Zoom) + PanX, (diagramY * Zoom) + PanY);
}
