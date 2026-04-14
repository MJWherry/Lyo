using System.Globalization;
using CsvHelper.Configuration;
using Lyo.Csv.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Csv;

public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary> Adds CSV service to the service collection. </summary>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCsvService()
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            services.AddSingleton<CsvService>(provider => {
                var logger = provider.GetService<ILogger<CsvService>>();
                return new(logger);
            });

            services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
            services.AddSingleton<ICsvExporter>(sp => sp.GetRequiredService<CsvService>().Exporter);
            services.AddSingleton<ICsvImporter>(sp => sp.GetRequiredService<CsvService>().Importer);
            return services;
        }

        /// <summary> Adds CSV service to the service collection with configuration. </summary>
        /// <param name="configure">Action to configure the CSV configuration</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCsvService(Action<CsvConfiguration> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<CsvService>(provider => {
                var logger = provider.GetService<ILogger<CsvService>>();
                var config = new CsvConfiguration(CultureInfo.InvariantCulture);
                configure(config);
                return new(logger, config);
            });

            services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
            services.AddSingleton<ICsvExporter>(sp => sp.GetRequiredService<CsvService>().Exporter);
            services.AddSingleton<ICsvImporter>(sp => sp.GetRequiredService<CsvService>().Importer);
            return services;
        }

        /// <summary> Adds CSV service to the service collection with configuration builder. </summary>
        /// <param name="configBuilder">Function that builds the CSV configuration</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCsvService(Func<CsvConfiguration> configBuilder)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configBuilder, nameof(configBuilder));
            services.AddSingleton<CsvService>(provider => {
                var logger = provider.GetService<ILogger<CsvService>>();
                return new(logger, configBuilder);
            });

            services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
            services.AddSingleton<ICsvExporter>(sp => sp.GetRequiredService<CsvService>().Exporter);
            services.AddSingleton<ICsvImporter>(sp => sp.GetRequiredService<CsvService>().Importer);
            return services;
        }

        /// <summary> Adds CSV service to the service collection with configuration that has access to the service provider. </summary>
        /// <param name="configure">Action that receives the service provider and CSV configuration to configure</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCsvService(Action<IServiceProvider, CsvConfiguration> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<CsvService>(provider => {
                var logger = provider.GetService<ILogger<CsvService>>();
                var config = new CsvConfiguration(CultureInfo.InvariantCulture);
                configure(provider, config);
                return new(logger, config);
            });

            services.AddSingleton<ICsvService>(sp => sp.GetRequiredService<CsvService>());
            services.AddSingleton<ICsvExporter>(sp => sp.GetRequiredService<CsvService>().Exporter);
            services.AddSingleton<ICsvImporter>(sp => sp.GetRequiredService<CsvService>().Importer);
            return services;
        }
    }
}