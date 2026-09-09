using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Selenium.Configuration;

namespace Lyo.Web.Automation.Selenium.Tests;

public sealed class SeleniumBrowserOptionsTests
{
    [Fact]
    public void Defaults_AreNotHeadlessFingerprint()
    {
        var options = new SeleniumBrowserOptions();
        Assert.DoesNotContain(options.WebDriverArguments, a => a.Contains("disable-gpu", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1920, options.BrowserWindowWidth);
        Assert.Equal(1080, options.BrowserWindowHeight);
    }

    [Fact]
    public void ApplySessionViewport_Randomize_UsesPool()
    {
        var options = new SeleniumBrowserOptions {
            RandomizeViewportOnSessionStart = true,
            ViewportSizes = [new DesktopDisplaySize(1366, 768)],
            BrowserWindowWidth = 800,
            BrowserWindowHeight = 600
        };
        options.ApplySessionViewport(new Random(1));
        Assert.Equal(1366, options.BrowserWindowWidth);
        Assert.Equal(768, options.BrowserWindowHeight);
    }

    [Fact]
    public void Builder_RandomizeViewportOnSessionStart_CopiesToOptions()
    {
        var options = SeleniumBrowserOptionsBuilder.New().RandomizeViewportOnSessionStart().ViewportJitterPixels(2).Build();
        Assert.True(options.RandomizeViewportOnSessionStart);
        Assert.Equal(2, options.ViewportJitterPixels);
    }
}
