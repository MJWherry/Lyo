using Lyo.Compression.Models;
using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Compression;

public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds compression service to the service collection with default options.</summary>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCompressionService()
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            services.AddSingleton<CompressionServiceOptions>(_ => new());
            services.AddSingleton<CompressionService>();
            services.AddSingleton<ICompressionService>(provider => provider.GetRequiredService<CompressionService>());
            return services;
        }

        /// <summary>Adds compression service to the service collection.</summary>
        /// <param name="configure">Action to configure the options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCompressionService(Action<CompressionServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<CompressionServiceOptions>(_ => {
                var options = new CompressionServiceOptions();
                configure(options);
                return options;
            });

            services.AddSingleton<CompressionService>();
            services.AddSingleton<ICompressionService>(provider => provider.GetRequiredService<CompressionService>());
            return services;
        }

        /// <summary>Adds compression service to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="configSectionName">The configuration section name (defaults to "CompressionService")</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCompressionServiceFromConfiguration(IConfiguration configuration, string configSectionName = "CompressionService")
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
            services.AddOptions<CompressionServiceOptions>(configSectionName)
                .Configure<IConfiguration>((options, config) => {
                    config.GetSection(configSectionName).Bind(options);
                })
                .ValidateOnStart();

            services.AddSingleton<CompressionService>();
            services.AddSingleton<ICompressionService>(provider => provider.GetRequiredService<CompressionService>());
            return services;
        }

        /// <summary>Adds compression service to the service collection as a keyed service.</summary>
        /// <param name="keyedServiceName">The key name for the keyed service</param>
        /// <param name="configure">Optional action to configure the options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddCompressionServiceKeyed(string keyedServiceName, Action<CompressionServiceOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyedServiceName, nameof(keyedServiceName));
            services.AddKeyedSingleton<CompressionServiceOptions>(
                keyedServiceName, (_, _) => {
                    var options = new CompressionServiceOptions();
                    configure?.Invoke(options);
                    return options;
                });

            services.AddKeyedSingleton<CompressionService>(keyedServiceName);
            services.AddKeyedSingleton<ICompressionService>(keyedServiceName, (provider, _) => provider.GetRequiredKeyedService<CompressionService>(keyedServiceName));
            return services;
        }
    }
}