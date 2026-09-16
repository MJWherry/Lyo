using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Idempotency;

/// <summary>DI registration for the in-memory idempotency store.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the store.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="InMemoryIdempotencyStore" /> behind <see cref="ObservingIdempotencyStore" />.
        /// A missing <see cref="IMetrics" /> or <see cref="ILoggerFactory" /> does not fail startup.
        /// </summary>
        public IServiceCollection AddInMemoryIdempotency(Func<DateTime>? utcNow = null, IdempotencyOptions? options = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var configured = options ?? new IdempotencyOptions();
            services.TryAddSingleton(configured);
            services.AddSingleton(sp => new InMemoryIdempotencyStore(configured, utcNow));
            services.AddSingleton<IIdempotencyStore>(sp => new ObservingIdempotencyStore(
                sp.GetRequiredService<InMemoryIdempotencyStore>(),
                sp.GetService<IMetrics>(),
                sp.GetService<ILoggerFactory>()?.CreateLogger<ObservingIdempotencyStore>()));
            return services;
        }
    }
}
