using Lyo.Exceptions;
using Lyo.IO.Temp.Models;
using Lyo.IO.FileSystem;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.IO.Temp;

/// <summary>DI helpers that register the IO temp service.</summary>
public static class Extensions
{
    private static void ConfigureCleanupOptions(IServiceCollection services, TimeSpan? interval, TimeSpan? delay)
    {
        if (interval.HasValue || delay.HasValue) {
            services.Configure<IOTempCleanupOptions>(o => {
                if (interval.HasValue)
                    o.Interval = interval.Value;

                if (delay.HasValue)
                    o.InitialDelay = delay.Value;
            });
            services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<IOTempCleanupOptions>>().Value);
        }
    }

    private static IOTempService CreateService(IServiceProvider provider)
    {
        var logger = provider.GetService<ILogger<IOTempService>>();
        var loggerFactory = provider.GetService<ILoggerFactory>();
        var metrics = provider.GetService<IMetrics>();
        var options = provider.GetRequiredService<IOTempServiceOptions>();
        // Prefer a registered IFileSystem; otherwise use the filesystem provider.
        var storageProvider = provider.GetService<IFileSystem>() ?? IOTempService.CreateDefaultStorage(options);
        return new(options, logger, metrics, loggerFactory, storageProvider);
    }

    extension(IServiceCollection services)
    {
        /// <summary>Registers IO temp with default options.</summary>
        public IServiceCollection AddIOTempService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new IOTempServiceOptions();
            options.Validate();
            services.TryAddSingleton(options);
            services.AddSingleton(CreateService);
            services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
            return services;
        }

        /// <summary>Registers IO temp and runs <paramref name="configure" /> on the options instance.</summary>
        public IServiceCollection AddIOTempService(Action<IOTempServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new IOTempServiceOptions();
            configure(options);
            options.Validate();
            services.TryAddSingleton(options);
            services.AddSingleton(CreateService);
            services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
            return services;
        }

        /// <summary>Registers IO temp with options bound from configuration.</summary>
        public IServiceCollection AddIOTempServiceFromConfiguration(IConfiguration configuration, string configSectionName = IOTempServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new IOTempServiceOptions();
            configuration.GetSection(configSectionName).Bind(options);
            options.Validate();
            services.TryAddSingleton(options);
            services.AddSingleton(CreateService);
            services.AddSingleton<IIOTempService>(provider => provider.GetRequiredService<IOTempService>());
            return services;
        }

        /// <summary>
        /// Registers <see cref="IIOTempService" /> and hosts <see cref="IOTempCleanupWorker" />, which calls <see cref="IIOTempService.Cleanup" /> on a timer.
        /// </summary>
        public IServiceCollection AddIOTempServiceWithAutoCleanup(TimeSpan? cleanupInterval = null, TimeSpan? initialDelay = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddIOTempService();
            ConfigureCleanupOptions(services, cleanupInterval, initialDelay);
            services.AddHostedService<IOTempCleanupWorker>();
            return services;
        }

        /// <summary>Registers <see cref="IIOTempService" /> with an options callback and hosts <see cref="IOTempCleanupWorker" />.</summary>
        public IServiceCollection AddIOTempServiceWithAutoCleanup(Action<IOTempServiceOptions> configureService, TimeSpan? cleanupInterval = null, TimeSpan? initialDelay = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configureService);
            services.AddIOTempService(configureService);
            ConfigureCleanupOptions(services, cleanupInterval, initialDelay);
            services.AddHostedService<IOTempCleanupWorker>();
            return services;
        }
    }
}