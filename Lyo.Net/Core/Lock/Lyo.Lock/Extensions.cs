using Lyo.Exceptions;
using Lyo.Lock.Abstractions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Lock;

/// <summary>Registers <see cref="LocalLockService" /> and <see cref="LocalKeyedSemaphoreService" /> as singletons, with optional options binding.</summary>
public static class LockServiceExtensions
{
    /// <param name="services">The application service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="ILockService" /> as a singleton <see cref="LocalLockService" />.</summary>
        /// <param name="configureOptions">Optional callback after defaults are applied.</param>
        /// <returns>The same collection, for chaining.</returns>
        public IServiceCollection AddLocalLock(Action<LockOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new LockOptions();
            configureOptions?.Invoke(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<ILockService>(sp => {
                var logger = sp.GetService<ILogger<LocalLockService>>();
                var opts = sp.GetRequiredService<LockOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() : null;
                return new LocalLockService(logger, opts, metrics);
            });

            return services;
        }

        /// <summary>
        /// Binds <see cref="LockOptions" /> from the <c>LockOptions</c> section (see <see cref="LockOptions.SectionName" />), then registers <see cref="LocalLockService" />.
        /// </summary>
        /// <param name="configuration">Configuration root or section parent.</param>
        /// <param name="configureOptions">Optional extra changes after bind.</param>
        public IServiceCollection AddLocalLockFromConfiguration(IConfiguration configuration, Action<LockOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new LockOptions();
            configuration.GetSection(LockOptions.SectionName).Bind(options);
            configureOptions?.Invoke(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<ILockService>(sp => {
                var logger = sp.GetService<ILogger<LocalLockService>>();
                var opts = sp.GetRequiredService<LockOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() : null;
                return new LocalLockService(logger, opts, metrics);
            });

            return services;
        }

        /// <summary>Registers <see cref="IKeyedSemaphoreService" /> as a singleton <see cref="LocalKeyedSemaphoreService" />.</summary>
        /// <param name="configureOptions">Optional callback after defaults are applied.</param>
        public IServiceCollection AddLocalKeyedSemaphore(Action<KeyedSemaphoreOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new KeyedSemaphoreOptions();
            configureOptions?.Invoke(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<IKeyedSemaphoreService>(sp => {
                var logger = sp.GetService<ILogger<LocalKeyedSemaphoreService>>();
                var opts = sp.GetRequiredService<KeyedSemaphoreOptions>();
                var metrics = opts.EnableMetrics ? sp.GetService<IMetrics>() : null;
                return new LocalKeyedSemaphoreService(logger, opts, metrics);
            });

            return services;
        }

        /// <summary>
        /// Binds <see cref="KeyedSemaphoreOptions" /> from <see cref="KeyedSemaphoreOptions.SectionName" />, then registers <see cref="LocalKeyedSemaphoreService" />.
        /// </summary>
        /// <param name="configuration">Configuration root or section parent.</param>
        /// <param name="configureOptions">Optional extra changes after bind.</param>
        public IServiceCollection AddLocalKeyedSemaphoreFromConfiguration(IConfiguration configuration, Action<KeyedSemaphoreOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new KeyedSemaphoreOptions();
            configuration.GetSection(KeyedSemaphoreOptions.SectionName).Bind(options);
            configureOptions?.Invoke(options);
            options.Validate();
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