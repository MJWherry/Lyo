using Lyo.Exceptions;
using Lyo.Idempotency.Postgres;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Outbox.Dispatcher;

/// <summary>DI registration for <see cref="OutboxDispatcher" />.</summary>
public static class Extensions
{
    /// <param name="services">Collection that already has <see cref="PostgresIdempotencyStore" /> and <see cref="IMqService" />.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the dispatcher hosted service. Does not register a queue client.</summary>
        public IServiceCollection AddOutboxDispatcher(OutboxDispatcherOptions? options = null, Func<DateTime>? utcNow = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            var configured = options ?? new OutboxDispatcherOptions();
            configured.Validate();
            services.AddSingleton(configured);
            services.AddHostedService(sp => new OutboxDispatcher(
                configured,
                sp.GetRequiredService<PostgresIdempotencyStore>(),
                sp.GetRequiredService<IMqService>(),
                sp.GetService<IMetrics>(),
                sp.GetService<ILoggerFactory>()?.CreateLogger<OutboxDispatcher>(),
                utcNow));
            return services;
        }

        /// <summary>Binds <see cref="OutboxDispatcherOptions" /> and registers the dispatcher.</summary>
        public IServiceCollection AddOutboxDispatcherFromConfiguration(IConfiguration configuration, string sectionName = OutboxDispatcherOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new OutboxDispatcherOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddOutboxDispatcher(options);
        }
    }
}
