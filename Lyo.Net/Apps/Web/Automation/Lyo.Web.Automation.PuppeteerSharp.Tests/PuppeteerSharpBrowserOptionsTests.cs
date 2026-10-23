using Lyo.Common.Core.Net;
using Lyo.Exceptions.Models;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.PuppeteerSharp.Configuration;

namespace Lyo.Web.Automation.PuppeteerSharp.Tests;

public sealed class PuppeteerSharpBrowserOptionsTests
{
    [Fact]
    public void ResolveLaunchArguments_BareToken_Throws()
    {
        var options = new PuppeteerSharpBrowserOptions { LaunchArguments = ["http://1920,1080"] };
        Assert.Throws<ArgumentException>(options.ResolveLaunchArguments);
    }

    [Fact]
    public void ResolveLaunchArguments_Firefox_OmitsChromiumDefaults()
    {
        var options = new PuppeteerSharpBrowserOptions { BrowserKind = AutomationBrowserKind.Firefox, ScreenWidth = 1920, ScreenHeight = 1080 };
        var args = options.ResolveLaunchArguments();
        Assert.DoesNotContain(args, a => a.Contains("window-size", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(args, a => a.Contains("no-sandbox", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(args);
    }

    [Fact]
    public void Validate_WebKit_Throws()
    {
        var options = new PuppeteerSharpBrowserOptions { BrowserKind = AutomationBrowserKind.WebKit, BrowserExePath = null };
        var ex = Assert.Throws<ConfigurationException>(options.Validate);
        Assert.Contains("WebKit", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveContextUserAgent_Firefox_UsesGeneratedAgent()
    {
        var options = new PuppeteerSharpBrowserOptions { BrowserKind = AutomationBrowserKind.Firefox, UserAgents = [] };
        var ua = options.ResolveContextUserAgent();
        Assert.Contains($"Firefox/{UserAgentGenerator.FirefoxVersion}", ua, StringComparison.Ordinal);
        Assert.DoesNotContain("Chrome/", ua, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplySessionViewport_Randomize_AssignsWidthAndHeight()
    {
        var options = new PuppeteerSharpBrowserOptions {
            RandomizeViewportOnSessionStart = true,
            ViewportSizes = [new(1440, 900)],
            ViewportWidth = 800,
            ViewportHeight = 600
        };

        options.ApplySessionViewport(new(1));
        Assert.Equal(1440, options.ViewportWidth);
        Assert.Equal(900, options.ViewportHeight);
        Assert.Equal(1440, options.ScreenWidth);
        Assert.Equal(900, options.ScreenHeight);
    }
}