using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.WebRenderer.Tests;

internal static class WebRendererTestHost
{
    public static WebRendererService CreateService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var htmlRenderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return new WebRendererService(htmlRenderer);
    }
}
