using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Playwright.Tests;

public sealed class PlaywrightServiceRegistrationTests
{
    [Fact]
    public void AddPlaywrightBrowserService_ResolvesService()
    {
        var services = new ServiceCollection();
        services.AddPlaywrightBrowserService(options => options.Headless = true);
        using var provider = services.BuildServiceProvider();
        var service = provider.GetService<IPlaywrightBrowserService>();
        Assert.NotNull(service);
    }
}