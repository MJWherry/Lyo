using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Web;
using Lyo.Reporting.Web.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.WebRenderer.Tests;

public sealed class ReportingWebRendererDiTests
{
    [Fact]
    public void AddReportingWebRenderer_ScopedWebRenderer_Validates()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<HtmlRenderer>(sp => new(sp, sp.GetRequiredService<ILoggerFactory>()));
        services.AddScoped<IWebRendererService>(sp => new WebRendererService(sp.GetRequiredService<HtmlRenderer>()));
        services.AddReportingWebRenderer();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        Assert.Contains(scope.ServiceProvider.GetServices<IReportRenderer>(), r => r is HtmlPdfReportRenderer);
    }
}
