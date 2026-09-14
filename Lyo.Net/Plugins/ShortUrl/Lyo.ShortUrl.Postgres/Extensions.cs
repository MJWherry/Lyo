using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.ShortUrl.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.ShortUrl.Postgres;

/// <summary>DI helpers for PostgreSQL URL shortener database context registration.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers ShortUrlDbContext to the service collection.</summary>
        /// <param name="connectionString">Postgres connection string</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddShortUrlDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddShortUrlDbContextFactory(new PostgresShortUrlOptions { ConnectionString = connectionString })
                .AddScoped<ShortUrlDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ShortUrlDbContext>>().CreateDbContext());
        }

        /// <summary>Registers PostgreSQL URL shortener DbContextFactory to the service collection.</summary>
        /// <param name="configure">Callback that fills Postgres short-URL options</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddShortUrlDbContextFactory(Action<PostgresShortUrlOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresShortUrlOptions();
            configure(options);
            return services.AddShortUrlDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL URL shortener DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">Host configuration (for example builder.Configuration)</param>
        /// <param name="configSectionName">Configuration section name; defaults to PostgresShortUrlOptions.SectionName</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddShortUrlDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresShortUrlOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresShortUrlOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddShortUrlDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL URL shortener DbContextFactory to the service collection.</summary>
        /// <param name="options">Postgres short-URL options</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddShortUrlDbContextFactory(PostgresShortUrlOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<ShortUrlDbContext, PostgresShortUrlOptions>(options);

            return services;
        }
    }
}