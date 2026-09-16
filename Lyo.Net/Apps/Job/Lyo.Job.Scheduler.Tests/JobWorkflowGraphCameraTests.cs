using Lyo.Job.Web.Components;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// The diagram viewport pans in screen pixels and zooms around a cursor. These lock that transform so a scroll or a drag does not slide the point under the pointer.
/// </summary>
public class JobWorkflowGraphCameraTests
{
    [Fact]
    public void ZoomAt_Cursor_KeepsTheDiagramPointFixed()
    {
        var camera = new JobWorkflowGraphCamera();

        camera.ZoomAt(2, 100, 80);

        var (x, y) = camera.ToScreen(100, 80);
        Assert.Equal(2, camera.Zoom);
        Assert.Equal(100, x, precision: 6);
        Assert.Equal(80, y, precision: 6);
    }

    [Fact]
    public void ZoomAt_AfterPan_KeepsThePointUnderTheCursor()
    {
        var camera = new JobWorkflowGraphCamera();
        camera.Pan(30, 10);

        camera.ZoomAt(2, 90, 50);

        var (x, y) = camera.ToScreen(60, 40);
        Assert.Equal(90, x, precision: 6);
        Assert.Equal(50, y, precision: 6);
    }

    [Fact]
    public void ZoomAt_PastTheCap_StopsAndStillHoldsTheCursor()
    {
        var camera = new JobWorkflowGraphCamera();

        camera.ZoomAt(100, 40, 20);

        Assert.Equal(JobWorkflowGraphCamera.MaxZoom, camera.Zoom);
        var (x, y) = camera.ToScreen(40, 20);
        Assert.Equal(40, x, precision: 6);
        Assert.Equal(20, y, precision: 6);
    }

    [Fact]
    public void Pan_AddsThePointerDelta()
    {
        var camera = new JobWorkflowGraphCamera();

        camera.Pan(12, -5);
        camera.Pan(3, 1);

        Assert.Equal(15, camera.PanX);
        Assert.Equal(-4, camera.PanY);
    }

    [Fact]
    public void Reset_ReturnsToOneToOneAtTheGivenOrigin()
    {
        var camera = new JobWorkflowGraphCamera();
        camera.Pan(40, 12);
        camera.ZoomAt(2, 10, 10);

        camera.Reset(16, 16);

        Assert.Equal(1, camera.Zoom);
        Assert.Equal(16, camera.PanX);
        Assert.Equal(16, camera.PanY);
    }
}
