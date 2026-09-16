using Lyo.Audit;
using Lyo.Authorization.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authorization.Postgres;

/// <summary>DI registration for the Postgres grant store.</summary>
public static class Extensions
{
    /// <param name="services">Collection that receives the store.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the authorization context factory and <see cref="PostgresGrantStore" />.</summary>
        public IServiceCollection AddPostgresAuthorization(PostgresAuthorizationOptions options, Func<DateTime>? utcNow = null)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<AuthorizationDbContext, PostgresAuthorizationOptions>(options);
            services.TryAddSingleton(options);
            services.AddSingleton(sp => new PostgresGrantStore(sp.GetRequiredService<IDbContextFactory<AuthorizationDbContext>>(), utcNow));
            services.AddSingleton<IGrantStore>(sp => sp.GetRequiredService<PostgresGrantStore>());
            services.TryAddSingleton<IResourceAuthorization>(sp => new ResourceAuthorization(
                sp.GetRequiredService<IGrantStore>(),
                sp.GetService<IMetrics>(),
                sp.GetService<ILoggerFactory>()?.CreateLogger<ResourceAuthorization>(),
                sp.GetService<IAuditRecorder>()));
            return services;
        }

        /// <summary>Binds <see cref="PostgresAuthorizationOptions" /> and registers the store.</summary>
        public IServiceCollection AddPostgresAuthorizationFromConfiguration(IConfiguration configuration, string sectionName = PostgresAuthorizationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresAuthorizationOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddPostgresAuthorization(options);
        }
    }
}
