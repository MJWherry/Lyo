using System.Globalization;
using CsvHelper.Configuration;
using Lyo.Csv.Models;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Csv;

/// <summary>Registers <see cref="CsvService" /> and related CSV abstractions with <see cref="IServiceCollection" />.</summary>
public static class Extensions
{
    /// <summary>Adds CSV service with default options (CSV value pooling off).</summary>
    public static IServiceCollection AddCsvService(this IServiceCollection services)
        => services.AddCsvService(new CsvOptions());

    /// <summary>Adds CSV service configured by the given <see cref="CsvOptions" /> action.</summary>
    public static IServiceCollection AddCsvService(this IServiceCollection services, Action<CsvOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new CsvOptions();
        configure(options);
        return services.AddCsvService(options);
    }

    /// <summary>Adds CSV service from configuration (section <see cref="CsvOptions.SectionName" /> by default).</summary>
    public static IServiceCollection AddCsvServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = CsvOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        var options = new CsvOptions();
        var section = configuration.GetSection(sectionName);
        if (section.Exists())
            section.Bind(options);

        var poolingSection = configuration.GetSection(DataTablePoolingOptions.SectionName);
        if (poolingSection.Exists())
            poolingSection.Bind(options.Pooling);

        return services.AddCsvService(options);
    }

    /// <summary>Adds CSV service with the given options instance (default CsvHelper configuration).</summary>
    public static IServiceCollection AddCsvService(this IServiceCollection services, CsvOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<CsvService>(provider => {
            var logger = provider.GetService<ILogger<CsvService>>();
            var opts = provider.GetService<IOptions<CsvOptions>>()?.Value ?? options;
            return new(logger, csvConfiguration: null, httpClient: null, opts);
        });

        services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
        services.AddSingleton<ICsvWriter>(sp => sp.GetRequiredService<CsvService>().Writer);
        services.AddSingleton<ICsvReader>(sp => sp.GetRequiredService<CsvService>().Reader);
        return services;
    }

    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds CSV service with CsvHelper configuration (default <see cref="CsvOptions" /> pooling).</summary>
        /// <param name="configure">Action to configure the CSV configuration</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCsvService(Action<CsvConfiguration> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<CsvService>(provider => {
                var logger = provider.GetService<ILogger<CsvService>>();
                var config = new CsvConfiguration(CultureInfo.InvariantCulture);
                configure(config);
                return new(logger, config);
            });

            services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
            services.AddSingleton<ICsvWriter>(sp => sp.GetRequiredService<CsvService>().Writer);
            services.AddSingleton<ICsvReader>(sp => sp.GetRequiredService<CsvService>().Reader);
            return services;
        }

        /// <summary>Adds CSV service with a CsvHelper configuration builder (default <see cref="CsvOptions" /> pooling).</summary>
        /// <param name="configBuilder">Function that builds the CSV configuration</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCsvService(Func<CsvConfiguration> configBuilder)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configBuilder);
            services.AddSingleton<CsvService>(provider => {
                var logger = provider.GetService<ILogger<CsvService>>();
                return new(logger, configBuilder);
            });

            services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
            services.AddSingleton<ICsvWriter>(sp => sp.GetRequiredService<CsvService>().Writer);
            services.AddSingleton<ICsvReader>(sp => sp.GetRequiredService<CsvService>().Reader);
            return services;
        }

        /// <summary>Adds CSV service with CsvHelper configuration that has access to the service provider.</summary>
        /// <param name="configure">Action that receives the service provider and CSV configuration to configure</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCsvService(Action<IServiceProvider, CsvConfiguration> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<CsvService>(provider => {
                var logger = provider.GetService<ILogger<CsvService>>();
                var config = new CsvConfiguration(CultureInfo.InvariantCulture);
                configure(provider, config);
                return new(logger, config);
            });

            services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
            services.AddSingleton<ICsvWriter>(sp => sp.GetRequiredService<CsvService>().Writer);
            services.AddSingleton<ICsvReader>(sp => sp.GetRequiredService<CsvService>().Reader);
            return services;
        }
    }
}
