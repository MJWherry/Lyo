using Lyo.Web.Automation.PuppeteerSharp.Service;

namespace Lyo.Web.Automation.PuppeteerSharp.Tests;

public sealed class PuppeteerSharpBrowserServiceTests
{
    [Fact]
    public void CreateSession_Action_KeepsUnspecifiedOptions()
    {
        using var service = new PuppeteerSharpBrowserService(new() { ProxyUrl = "http://127.0.0.1:8888" });
        using var session = service.CreateSession(o => o.UserAgents = ["pinned"]);
        Assert.Equal("http://127.0.0.1:8888", session.Browser.Options.ProxyUrl);
        Assert.Equal(["pinned"], session.Browser.Options.UserAgents);
    }
}