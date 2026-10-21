using Lyo.Exceptions;
using Lyo.Idempotency.Postgres.Database;
using Lyo.Metrics;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Idempotency.Postgres;

/// <summary>DI registration for the Postgres idempotency store.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the store.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the idempotency context factory and <see cref="PostgresIdempotencyStore" />.</summary>
        public IServiceCollection AddPostgresIdempotency(PostgresIdempotencyOptions options, Func<DateTime>? utcNow = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<IdempotencyDbContext, PostgresIdempotencyOptions>(options);
            services.TryAddSingleton(options);
            services.AddSingleton(sp => new PostgresIdempotencyStore(options, utcNow, sp.GetService<IMetrics>()));
            services.AddSingleton<IIdempotencyStore>(sp => new ObservingIdempotencyStore(
                sp.GetRequiredService<PostgresIdempotencyStore>(), sp.GetService<IMetrics>(), sp.GetService<ILoggerFactory>()?.CreateLogger<ObservingIdempotencyStore>()));

            return services;
        }

        /// <summary>Binds <see cref="PostgresIdempotencyOptions" /> and registers the store.</summary>
        public IServiceCollection AddPostgresIdempotencyFromConfiguration(IConfiguration configuration, string sectionName = PostgresIdempotencyOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresIdempotencyOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddPostgresIdempotency(options);
        }
    }
}