using Lyo.Web.Automation.Selenium.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.Selenium.Tests;

public sealed class SeleniumServiceRegistrationTests
{
    [Fact]
    public void AddSeleniumBrowserService_ResolvesService()
    {
        var services = new ServiceCollection();
        services.AddSeleniumBrowserService(options => options.Headless = true);
        using var provider = services.BuildServiceProvider();

        var service = provider.GetService<ISeleniumBrowserService>();

        Assert.NotNull(service);
    }
}
