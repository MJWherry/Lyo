using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Lock;

/// <summary>Extension methods for configuring lock services with dependency injection.</summary>
public static class LockServiceExtensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds LocalLockService (in-memory, per-process locking).</summary>
        /// <param name="configureOptions">Optional action to configure LockOptions</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLocalLock(Action<LockOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            var options = new LockOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<ILockService>(sp => {
                var logger = sp.GetService<ILogger<LocalLockService>>();
                var opts = sp.GetRequiredService<LockOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() : null;
                return new LocalLockService(logger, opts, metrics);
            });

            return services;
        }

        /// <summary>Adds LocalLockService with configuration from IConfiguration.</summary>
        public IServiceCollection AddLocalLockFromConfiguration(IConfiguration configuration, Action<LockOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            var options = new LockOptions();
            var section = configuration.GetSection(LockOptions.SectionName);
            if (section.Exists())
                section.Bind(options);

            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<ILockService>(sp => {
                var logger = sp.GetService<ILogger<LocalLockService>>();
                var opts = sp.GetRequiredService<LockOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() : null;
                return new LocalLockService(logger, opts, metrics);
            });

            return services;
        }

        /// <summary>Adds LocalKeyedSemaphoreService (in-memory, per-process bounded concurrency).</summary>
        /// <param name="configureOptions">Optional action to configure KeyedSemaphoreOptions</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddLocalKeyedSemaphore(Action<KeyedSemaphoreOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            var options = new KeyedSemaphoreOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IKeyedSemaphoreService>(sp => {
                var logger = sp.GetService<ILogger<LocalKeyedSemaphoreService>>();
                var opts = sp.GetRequiredService<KeyedSemaphoreOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() : null;
                return new LocalKeyedSemaphoreService(logger, opts, metrics);
            });

            return services;
        }

        /// <summary>Adds LocalKeyedSemaphoreService with configuration from IConfiguration.</summary>
        public IServiceCollection AddLocalKeyedSemaphoreFromConfiguration(IConfiguration configuration, Action<KeyedSemaphoreOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            var options = new KeyedSemaphoreOptions();
            var section = configuration.GetSection(KeyedSemaphoreOptions.SectionName);
            if (section.Exists())
                section.Bind(options);

            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IKeyedSemaphoreService>(sp => {
                var logger = sp.GetService<ILogger<LocalKeyedSemaphoreService>>();
                var opts = sp.GetRequiredService<KeyedSemaphoreOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() : null;
                return new LocalKeyedSemaphoreService(logger, opts, metrics);
            });

            return services;
        }
    }
}