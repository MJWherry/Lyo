using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Preview;

/// <summary>Extension methods for registering the preview service.</summary>
public static class Extensions
{
    /// <summary>Adds the preview service to the service collection. For CSV and XLSX table preview, register ICsvService and IXlsxService first (e.g. AddCsvService(), AddXlsxService()).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPreviewService(this IServiceCollection services)
    {
        services.AddSingleton<IPreviewService>(sp => new BrowserPreview(sp.GetService<ILogger<BrowserPreview>>(), sp.GetRequiredService<IServiceScopeFactory>()));
        return services;
    }
}