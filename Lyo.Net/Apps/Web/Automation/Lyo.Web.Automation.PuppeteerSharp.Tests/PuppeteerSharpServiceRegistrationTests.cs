using Lyo.Web.Automation.PuppeteerSharp.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.PuppeteerSharp.Tests;

public sealed class PuppeteerSharpServiceRegistrationTests
{
    [Fact]
    public void AddPuppeteerSharpBrowserService_ResolvesSession()
    {
        var services = new ServiceCollection();
        services.AddPuppeteerSharpBrowserService(options => options.Headless = true);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IPuppeteerSharpBrowserService>();
        using var session = service.CreateSession();
        Assert.NotNull(session);
    }
}