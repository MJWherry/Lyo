using Lyo.Web.Automation.Playwright.Configuration;
using Lyo.Web.Automation.Playwright.Service;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class PlaywrightBrowserServiceTests
{
    [Fact]
    public void CreateSession_Configure_Keeps_Service_Browser_Kind()
    {
        using var service = new PlaywrightBrowserService(new PlaywrightBrowserOptions { BrowserKind = PlaywrightBrowserKind.Firefox });
        PlaywrightBrowserKind? seen = null;
        using var session = service.CreateSession(o => {
            seen = o.BrowserKind;
            o.UserAgents = ["pinned"];
        });
        Assert.Equal(PlaywrightBrowserKind.Firefox, seen);
        Assert.Equal(["pinned"], session.Browser.Options.UserAgents);
    }

    [Fact]
    public void CreateSession_Blank_Session_Options_Defaults_To_Chromium()
    {
        using var service = new PlaywrightBrowserService(new PlaywrightBrowserOptions { BrowserKind = PlaywrightBrowserKind.Firefox });
        using var session = service.CreateSession(new PlaywrightSessionOptions());
        Assert.Equal(PlaywrightBrowserKind.Chromium, session.Browser.Options.BrowserKind);
    }
}
