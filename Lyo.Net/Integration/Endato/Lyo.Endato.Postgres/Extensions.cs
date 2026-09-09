using Lyo.Configuration;
using Lyo.Endato.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Endato.Postgres;

/// <summary>DI helpers for PostgreSQL Endato database context registration.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers EndatoDbContext on the service collection.</summary>
        /// <param name="connectionString">Postgres connection string</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddEndatoDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddEndatoDbContextFactory(new PostgresEndatoOptions { ConnectionString = connectionString })
                .AddScoped<EndatoDbContext>(sp => sp.GetRequiredService<IDbContextFactory<EndatoDbContext>>().CreateDbContext());
        }

        /// <summary>Registers PostgreSQL Endato DbContextFactory to the service collection.</summary>
        /// <param name="configure">Callback that fills Postgres Endato options</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddEndatoDbContextFactory(Action<PostgresEndatoOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresEndatoOptions();
            configure(options);
            return services.AddEndatoDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL Endato DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">Host configuration (for example builder.Configuration)</param>
        /// <param name="configSectionName">Configuration section name; defaults to PostgresEndatoOptions.SectionName</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddEndatoDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresEndatoOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresEndatoOptions>(configuration, configSectionName);

            return services.AddEndatoDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL Endato DbContextFactory to the service collection.</summary>
        /// <param name="options">Postgres Endato options</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddEndatoDbContextFactory(PostgresEndatoOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<EndatoDbContext, PostgresEndatoOptions>(options);

            return services;
        }
    }
}