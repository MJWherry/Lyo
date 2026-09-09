using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Playwright.Configuration;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class PlaywrightBrowserOptionsTests
{
    [Fact]
    public void Defaults_AreNotHeadlessFingerprint()
    {
        var options = new PlaywrightBrowserOptions();
        Assert.DoesNotContain(options.LaunchArguments, a => a.Contains("disable-gpu", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1920, options.ViewportWidth);
        Assert.Equal(1080, options.ViewportHeight);
        Assert.Equal(1920, options.ScreenWidth);
        Assert.Equal(1080, options.ScreenHeight);
        Assert.False(options.RandomizeViewportOnSessionStart);
        Assert.Equal(WaitUntilState.DOMContentLoaded, options.NavigationWaitUntil);
    }

    [Fact]
    public void ApplySessionViewport_RaisesScreenToCoverViewport()
    {
        var options = new PlaywrightBrowserOptions { ViewportWidth = 2560, ViewportHeight = 1440, ScreenWidth = 1920, ScreenHeight = 1080 };
        options.ApplySessionViewport();
        Assert.Equal(2560, options.ScreenWidth);
        Assert.Equal(1440, options.ScreenHeight);
    }

    [Fact]
    public void ApplySessionViewport_Randomize_UsesSeededPool()
    {
        var options = new PlaywrightBrowserOptions {
            RandomizeViewportOnSessionStart = true,
            ViewportSizes = [new DesktopDisplaySize(1440, 900)],
            ViewportWidth = 800,
            ViewportHeight = 600
        };
        options.ApplySessionViewport(new Random(1));
        Assert.Equal(1440, options.ViewportWidth);
        Assert.Equal(900, options.ViewportHeight);
        Assert.Equal(1440, options.ScreenWidth);
        Assert.Equal(900, options.ScreenHeight);
    }

    [Fact]
    public void ResolveLaunchArguments_AddsWindowSizeOnce()
    {
        var options = new PlaywrightBrowserOptions { ScreenWidth = 1600, ScreenHeight = 900 };
        var first = options.ResolveLaunchArguments();
        Assert.Contains("--window-size=1600,900", first);
        options.LaunchArguments.Add("--window-size=1280,720");
        Assert.DoesNotContain(options.ResolveLaunchArguments(), a => a == "--window-size=1600,900");
    }

    [Fact]
    public void ResolveLaunchArguments_Firefox_OmitsWindowSizeAndChromiumDefaults()
    {
        var options = new PlaywrightBrowserOptions { BrowserKind = PlaywrightBrowserKind.Firefox, ScreenWidth = 1920, ScreenHeight = 1080 };
        var args = options.ResolveLaunchArguments();
        Assert.DoesNotContain(args, a => a.Contains("window-size", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(args, a => a.Contains("no-sandbox", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(args);
    }

    [Fact]
    public void ResolveLaunchArguments_Firefox_KeepsCallerArgsThatAreNotChromiumDefaults()
    {
        var options = new PlaywrightBrowserOptions {
            BrowserKind = PlaywrightBrowserKind.Firefox,
            LaunchArguments = ["--width=1280"]
        };
        Assert.Equal(["--width=1280"], options.ResolveLaunchArguments());
    }

    [Fact]
    public void ResolveLaunchArguments_BareToken_Throws()
    {
        var options = new PlaywrightBrowserOptions { LaunchArguments = ["http://1920,1080"] };
        Assert.Throws<ArgumentException>(options.ResolveLaunchArguments);
    }

    [Fact]
    public void ResolveContextUserAgent_Firefox_DefaultPool_IsNull()
    {
        var options = new PlaywrightBrowserOptions { BrowserKind = PlaywrightBrowserKind.Firefox };
        Assert.Null(options.ResolveContextUserAgent());
    }

    [Fact]
    public void ResolveContextUserAgent_Firefox_Custom_UsesFirst()
    {
        var options = new PlaywrightBrowserOptions {
            BrowserKind = PlaywrightBrowserKind.Firefox,
            UserAgents = ["Mozilla/5.0 (X11; Linux x86_64; rv:133.0) Gecko/20100101 Firefox/133.0"]
        };
        Assert.Equal("Mozilla/5.0 (X11; Linux x86_64; rv:133.0) Gecko/20100101 Firefox/133.0", options.ResolveContextUserAgent());
    }

    [Fact]
    public void Clone_CopiesProxy()
    {
        var options = new PlaywrightBrowserOptions { ProxyUrl = "http://127.0.0.1:8888", ProxyUsername = "u", ProxyPassword = "p" };
        var clone = options.Clone();
        Assert.Equal("http://127.0.0.1:8888", clone.ProxyUrl);
        Assert.Equal("u", clone.ProxyUsername);
        Assert.Equal("p", clone.ProxyPassword);
    }

    [Fact]
    public void Clone_CopiesViewportPool()
    {
        var options = new PlaywrightBrowserOptions {
            RandomizeViewportOnSessionStart = true,
            ViewportJitterPixels = 8,
            ViewportSizes = [new DesktopDisplaySize(1366, 768)],
            NavigationWaitUntil = WaitUntilState.Load
        };
        var clone = options.Clone();
        Assert.True(clone.RandomizeViewportOnSessionStart);
        Assert.Equal(8, clone.ViewportJitterPixels);
        Assert.Equal(new DesktopDisplaySize(1366, 768), Assert.Single(clone.ViewportSizes));
        Assert.Equal(WaitUntilState.Load, clone.NavigationWaitUntil);
        clone.ViewportSizes[0] = new(1, 1);
        Assert.Equal(1366, options.ViewportSizes[0].Width);
    }
}
