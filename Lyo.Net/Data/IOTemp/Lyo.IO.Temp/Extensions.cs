using Lyo.Exceptions;
using Lyo.IO.Temp.Models;
using Lyo.IO.Temp.Storage;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.IO.Temp;

/// <summary>Extension methods for registering IO temp service with dependency injection.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds IO temp service with default options.</summary>
        public IServiceCollection AddIOTempService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IOTempServiceOptions>(_ => new());
            services.AddSingleton(CreateService);
            services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
            return services;
        }

        /// <summary>Adds IO temp service with options configuration callback.</summary>
        public IServiceCollection AddIOTempService(Action<IOTempServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<IOTempServiceOptions>(_ => {
                var options = new IOTempServiceOptions();
                configure(options);
                return options;
            });

            services.AddSingleton(CreateService);
            services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
            return services;
        }

        /// <summary>Adds IO temp service with options bound from configuration.</summary>
        public IServiceCollection AddIOTempServiceFromConfiguration(
            IConfiguration configuration,
            string configSectionName = IOTempServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddSingleton<IOTempServiceOptions>(_ => {
                var options = new IOTempServiceOptions();
                var section = configuration.GetSection(configSectionName);
                if (section.Exists())
                    section.Bind(options);

                return options;
            });

            services.AddSingleton(CreateService);
            services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
            return services;
        }

        /// <summary>
        /// Adds <see cref="IIOTempService" /> and registers <see cref="IOTempCleanupWorker" /> as a hosted background service that periodically calls
        /// <see cref="IIOTempService.Cleanup" />.
        /// </summary>
        public IServiceCollection AddIOTempServiceWithAutoCleanup(TimeSpan? cleanupInterval = null, TimeSpan? initialDelay = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddIOTempService();
            ConfigureCleanupOptions(services, cleanupInterval, initialDelay);
            services.AddHostedService<IOTempCleanupWorker>();
            return services;
        }

        /// <summary>Adds <see cref="IIOTempService" /> with an options callback and registers <see cref="IOTempCleanupWorker" />.</summary>
        public IServiceCollection AddIOTempServiceWithAutoCleanup(
            Action<IOTempServiceOptions> configureService,
            TimeSpan? cleanupInterval = null,
            TimeSpan? initialDelay = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configureService);
            services.AddIOTempService(configureService);
            ConfigureCleanupOptions(services, cleanupInterval, initialDelay);
            services.AddHostedService<IOTempCleanupWorker>();
            return services;
        }
    }

    private static void ConfigureCleanupOptions(IServiceCollection services, TimeSpan? interval, TimeSpan? delay)
    {
        if (interval.HasValue || delay.HasValue) {
            services.Configure<IOTempCleanupOptions>(o => {
                if (interval.HasValue)
                    o.Interval = interval.Value;

                if (delay.HasValue)
                    o.InitialDelay = delay.Value;
            });
        }
    }

    private static IOTempService CreateService(IServiceProvider provider)
    {
        var logger = provider.GetService<ILogger<IOTempService>>();
        var loggerFactory = provider.GetService<ILoggerFactory>();
        var metrics = provider.GetService<IMetrics>();
        var options = provider.GetRequiredService<IOTempServiceOptions>();
        // Resolve a registered IIOTempStorageProvider if present; otherwise default to the filesystem provider.
        var storageProvider = provider.GetService<IIOTempStorageProvider>() ?? new FileSystemIOTempStorageProvider(options.RootDirectory);
        return new(options, logger, metrics, loggerFactory, storageProvider);
    }
}