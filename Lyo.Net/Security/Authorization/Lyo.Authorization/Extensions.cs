using Lyo.Audit;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authorization;

/// <summary>DI registration for the in-memory grant store.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the store.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="InMemoryGrantStore" />, <see cref="IGrantStore" />, and <see cref="IResourceAuthorization" />.
        /// A missing metrics, logger, or audit recorder does not fail startup.
        /// </summary>
        public IServiceCollection AddInMemoryAuthorization(Func<DateTime>? utcNow = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton(sp => new InMemoryGrantStore(utcNow));
            services.AddSingleton<IGrantStore>(sp => sp.GetRequiredService<InMemoryGrantStore>());
            services.TryAddSingleton<IResourceAuthorization>(sp => new ResourceAuthorization(
                sp.GetRequiredService<IGrantStore>(),
                sp.GetService<IMetrics>(),
                sp.GetService<ILoggerFactory>()?.CreateLogger<ResourceAuthorization>(),
                sp.GetService<IAuditRecorder>()));
            return services;
        }
    }
}
