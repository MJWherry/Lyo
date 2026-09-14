using Lyo.Csv.Models;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Csv;

/// <summary>DI helpers that register <see cref="CsvService" /> and related CSV abstractions with <see cref="IServiceCollection" />.</summary>
public static class Extensions
{
    /// <summary>Adds the CSV service with default options (CSV value pooling off).</summary>
    public static IServiceCollection AddCsvService(this IServiceCollection services) => services.AddCsvService(new CsvOptions());

    /// <summary>Adds the CSV service configured by the given <see cref="CsvOptions" /> action.</summary>
    public static IServiceCollection AddCsvService(this IServiceCollection services, Action<CsvOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new CsvOptions();
        configure(options);
        return services.AddCsvService(options);
    }

    /// <summary>Adds CSV service from configuration (section <see cref="CsvOptions.SectionName" /> when omitted).</summary>
    public static IServiceCollection AddCsvServiceFromConfiguration(this IServiceCollection services, IConfiguration configuration, string sectionName = CsvOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        var options = new CsvOptions();
        configuration.GetSection(sectionName).Bind(options);

        var poolingSection = configuration.GetSection(DataTablePoolingOptions.SectionName);
        if (poolingSection.Exists())
            poolingSection.Bind(options.Pooling);

        return services.AddCsvService(options);
    }

    /// <summary>Adds the CSV service with the given options instance.</summary>
    public static IServiceCollection AddCsvService(this IServiceCollection services, CsvOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton(options);
        services.AddSingleton<CsvService>(provider => {
            var logger = provider.GetService<ILogger<CsvService>>();
            var opts = provider.GetService<IOptions<CsvOptions>>()?.Value ?? options;
            return new(logger, null, opts);
        });

        services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
        services.AddSingleton<ICsvWriter>(sp => sp.GetRequiredService<CsvService>().Writer);
        services.AddSingleton<ICsvReader>(sp => sp.GetRequiredService<CsvService>().Reader);
        return services;
    }

    /// <param name="services">Service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds CSV service with options configured via the service provider.</summary>
        /// <param name="configure">Action that receives the service provider and options to configure.</param>
        /// <returns>Service collection for chaining.</returns>
        public IServiceCollection AddCsvService(Action<IServiceProvider, CsvOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<CsvService>(provider => {
                var logger = provider.GetService<ILogger<CsvService>>();
                var options = new CsvOptions();
                configure(provider, options);
                options.Validate();
                return new(logger, null, options);
            });

            services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
            services.AddSingleton<ICsvWriter>(sp => sp.GetRequiredService<CsvService>().Writer);
            services.AddSingleton<ICsvReader>(sp => sp.GetRequiredService<CsvService>().Reader);
            return services;
        }
    }
}