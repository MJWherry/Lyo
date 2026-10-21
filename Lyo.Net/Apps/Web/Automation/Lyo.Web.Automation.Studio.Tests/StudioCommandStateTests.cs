namespace Lyo.Web.Automation.Studio.Tests;

public class StudioCommandStateTests
{
    [Fact]
    public void ClosedBrowser_DisablesGoAndRun_AndLeavesSaveEnabled()
    {
        var state = StudioCommandState.From(
            new() {
                HasSession = true,
                BrowserOpen = false,
                StepCount = 2,
                SelectedStep = 0
            });

        Assert.False(state.Go);
        Assert.False(state.Run);
        Assert.True(state.Save);
    }

    [Fact]
    public void Running_DisablesGoAndRun_AndEnablesStop()
    {
        var state = StudioCommandState.From(
            new() {
                HasSession = true,
                BrowserOpen = true,
                Playing = true,
                StepCount = 2,
                SelectedStep = 0
            });

        Assert.False(state.Go);
        Assert.False(state.Run);
        Assert.True(state.Stop);
    }

    [Fact]
    public void NoSelectedStep_DisablesRunThisStepAndMoveAndDelete()
    {
        var state = StudioCommandState.From(
            new() {
                HasSession = true,
                BrowserOpen = true,
                StepCount = 3,
                SelectedStep = -1
            });

        Assert.False(state.RunThisStep);
        Assert.False(state.Up);
        Assert.False(state.Down);
        Assert.False(state.Delete);
    }
}