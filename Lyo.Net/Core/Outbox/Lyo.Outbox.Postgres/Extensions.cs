using Lyo.Exceptions;
using Lyo.Idempotency.Postgres;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Outbox.Postgres;

/// <summary>DI registration for <see cref="PostgresOutbox" />.</summary>
public static class Extensions
{
    /// <param name="services">Collection that already has <see cref="PostgresIdempotencyStore" />.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="PostgresOutbox" />. Resolving it throws when <see cref="PostgresIdempotencyStore" /> was not registered.</summary>
        public IServiceCollection AddPostgresOutbox()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IOutbox>(sp => {
                var store = sp.GetService<PostgresIdempotencyStore>() ??
                    throw new InvalidOperationException("AddPostgresOutbox requires PostgresIdempotencyStore. Call AddPostgresIdempotency first.");

                return new PostgresOutbox(store, sp.GetService<IMetrics>(), sp.GetService<ILoggerFactory>()?.CreateLogger<PostgresOutbox>());
            });

            return services;
        }

        /// <summary>Binds nothing extra. Present so hosts can call it beside <c>AddPostgresIdempotencyFromConfiguration</c>.</summary>
        public IServiceCollection AddPostgresOutboxFromConfiguration(IConfiguration configuration)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            return services.AddPostgresOutbox();
        }
    }
}