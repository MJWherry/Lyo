using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Xlsx;

/// <summary>Extension methods for registering XLSX services.</summary>
public static class Extensions
{
    /// <summary>Adds XLSX service with default options (pooling enabled above the default cell threshold).</summary>
    public static IServiceCollection AddXlsxService(this IServiceCollection services)
        => services.AddXlsxService(new XlsxOptions());

    /// <summary>Adds XLSX service configured by the given action.</summary>
    public static IServiceCollection AddXlsxService(this IServiceCollection services, Action<XlsxOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new XlsxOptions();
        configure(options);
        return services.AddXlsxService(options);
    }

    /// <summary>Adds XLSX service from configuration (section <see cref="XlsxOptions.SectionName" /> by default).</summary>
    public static IServiceCollection AddXlsxServiceFromConfiguration(this IServiceCollection services, IConfiguration configuration, string sectionName = XlsxOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        var options = new XlsxOptions();
        var section = configuration.GetSection(sectionName);
        if (section.Exists())
            section.Bind(options);

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
        services.AddSingleton<XlsxService>(provider => {
            var logger = provider.GetService<ILogger<XlsxService>>();
            var opts = provider.GetService<IOptions<XlsxOptions>>()?.Value ?? options;
            return new(logger, excelDataTableConfiguration: null, opts);
        });

        services.AddSingleton<IXlsxService>(sp => sp.GetRequiredService<XlsxService>());
        services.AddSingleton<IXlsxWriter>(sp => sp.GetRequiredService<XlsxService>().Writer);
        services.AddSingleton<IXlsxReader>(sp => sp.GetRequiredService<XlsxService>().Reader);
        return services;
    }
}
