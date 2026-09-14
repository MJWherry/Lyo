using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Xlsx;

/// <summary>DI helpers that register <see cref="XlsxService" /> and related XLSX abstractions.</summary>
public static class Extensions
{
    /// <summary>Adds the XLSX service with default options (pooling on above the default cell threshold).</summary>
    public static IServiceCollection AddXlsxService(this IServiceCollection services) => services.AddXlsxService(new XlsxOptions());

    /// <summary>Adds the XLSX service configured by the given <see cref="XlsxOptions" /> action.</summary>
    public static IServiceCollection AddXlsxService(this IServiceCollection services, Action<XlsxOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new XlsxOptions();
        configure(options);
        return services.AddXlsxService(options);
    }

    /// <summary>Adds the XLSX service from configuration (section <see cref="XlsxOptions.SectionName" /> when omitted).</summary>
    public static IServiceCollection AddXlsxServiceFromConfiguration(this IServiceCollection services, IConfiguration configuration, string sectionName = XlsxOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        var options = new XlsxOptions();
        configuration.GetSection(sectionName).Bind(options);

        var poolingSection = configuration.GetSection(DataTablePoolingOptions.SectionName);
        if (poolingSection.Exists())
            poolingSection.Bind(options.Pooling);

        return services.AddXlsxService(options);
    }

    /// <summary>Adds XLSX service with the given options instance.</summary>
    public static IServiceCollection AddXlsxService(this IServiceCollection services, XlsxOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton(options);
        services.AddSingleton<XlsxService>(provider => {
            var logger = provider.GetService<ILogger<XlsxService>>();
            var opts = provider.GetService<IOptions<XlsxOptions>>()?.Value ?? options;
            return new(logger, null, opts);
        });

        services.AddSingleton<IXlsxService>(sp => sp.GetRequiredService<XlsxService>());
        services.AddSingleton<IXlsxWriter>(sp => sp.GetRequiredService<XlsxService>().Writer);
        services.AddSingleton<IXlsxReader>(sp => sp.GetRequiredService<XlsxService>().Reader);
        return services;
    }
}