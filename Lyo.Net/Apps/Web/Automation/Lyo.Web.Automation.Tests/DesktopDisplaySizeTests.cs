using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Tests;

public sealed class DesktopDisplaySizeTests
{
    [Fact]
    public void Pick_EmptyPool_UsesCommonAndSeededRandom_IsDeterministic()
    {
        var a = DesktopDisplaySize.Pick(null, 0, new Random(7));
        var b = DesktopDisplaySize.Pick([], 0, new Random(7));
        Assert.Equal(a, b);
        Assert.Contains(a, DesktopDisplaySize.Common);
        Assert.NotEqual(new DesktopDisplaySize(800, 600), a);
    }

    [Fact]
    public void Pick_Jitter_ClampsAboveHeadlessDefault()
    {
        var picked = DesktopDisplaySize.Pick([new(1024, 720)], jitterPixels: 10_000, random: new Random(1));
        Assert.True(picked.Width >= DesktopDisplaySize.MinWidth);
        Assert.True(picked.Height >= DesktopDisplaySize.MinHeight);
    }
}
