using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Xlsx;

/// <summary>Extension methods for registering XLSX services.</summary>
public static class Extensions
{
    /// <summary>Adds XLSX service to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddXlsxService(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddSingleton<XlsxService>(provider => {
            var logger = provider.GetService<ILogger<XlsxService>>();
            return new(logger);
        });

        services.AddSingleton<IXlsxService>(sp => sp.GetRequiredService<XlsxService>());
        services.AddSingleton<IXlsxExporter>(sp => sp.GetRequiredService<XlsxService>().Exporter);
        services.AddSingleton<IXlsxImporter>(sp => sp.GetRequiredService<XlsxService>().Importer);
        return services;
    }
}