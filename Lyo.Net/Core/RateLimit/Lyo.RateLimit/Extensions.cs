using Lyo.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.RateLimit;

/// <summary>Registers <see cref="LocalRateLimitService" />. A Redis registration already in the collection is left in place.</summary>
public static class RateLimitServiceExtensions
{
    /// <param name="services">Application service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers an in-memory <see cref="IRateLimitService" /> when none is registered yet.</summary>
        /// <param name="configureOptions">Optional callback after defaults are applied.</param>
        public IServiceCollection AddLocalRateLimit(Action<RateLimitOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var options = new RateLimitOptions();
            configureOptions?.Invoke(options);
            return services.AddLocalRateLimit(options);
        }

        /// <summary>Registers <paramref name="options" /> and an in-memory <see cref="IRateLimitService" /> when none is registered yet.</summary>
        /// <param name="options">Validated options instance.</param>
        public IServiceCollection AddLocalRateLimit(RateLimitOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(options);
            services.TryAddSingleton<IRateLimitService>(sp => new LocalRateLimitService(sp.GetRequiredService<RateLimitOptions>()));
            return services;
        }

        /// <summary>Binds <see cref="RateLimitOptions.SectionName" /> and registers the in-memory store when none is registered yet.</summary>
        /// <param name="configuration">Configuration root or section parent.</param>
        /// <param name="configureOptions">Optional changes after bind.</param>
        public IServiceCollection AddLocalRateLimitFromConfiguration(IConfiguration configuration, Action<RateLimitOptions>? configureOptions = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new RateLimitOptions();
            configuration.GetSection(RateLimitOptions.SectionName).Bind(options);
            configureOptions?.Invoke(options);
            return services.AddLocalRateLimit(options);
        }
    }
}
