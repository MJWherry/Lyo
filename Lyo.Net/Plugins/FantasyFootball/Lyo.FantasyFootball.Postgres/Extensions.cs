using Lyo.Exceptions;
using Lyo.FantasyFootball.Postgres.Database;
using Lyo.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FantasyFootball.Postgres;

/// <summary>DI registration for the fantasy football PostgreSQL store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the fantasy football DbContext factory from a configure delegate.</summary>
        public IServiceCollection AddFantasyFootballDbContextFactory(Action<PostgresFantasyFootballOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresFantasyFootballOptions();
            configure(options);
            return services.AddFantasyFootballDbContextFactory(options);
        }

        /// <summary>Registers the fantasy football DbContext factory from configuration.</summary>
        public IServiceCollection AddFantasyFootballDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresFantasyFootballOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresFantasyFootballOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddFantasyFootballDbContextFactory(options);
        }

        /// <summary>Registers the fantasy football DbContext factory.</summary>
        public IServiceCollection AddFantasyFootballDbContextFactory(PostgresFantasyFootballOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<FantasyFootballDbContext, PostgresFantasyFootballOptions>(options);
            return services;
        }

        /// <summary>Registers <see cref="IFantasyFootballStore" /> backed by PostgreSQL.</summary>
        public IServiceCollection AddPostgresFantasyFootballStore(Action<PostgresFantasyFootballOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresFantasyFootballOptions();
            configure(options);
            return services.AddPostgresFantasyFootballStore(options);
        }

        /// <summary>Registers <see cref="IFantasyFootballStore" /> from configuration.</summary>
        public IServiceCollection AddPostgresFantasyFootballStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresFantasyFootballOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresFantasyFootballOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddPostgresFantasyFootballStore(options);
        }

        /// <summary>Registers <see cref="IFantasyFootballStore" /> backed by PostgreSQL.</summary>
        public IServiceCollection AddPostgresFantasyFootballStore(PostgresFantasyFootballOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddFantasyFootballDbContextFactory(options);
            services.AddSingleton<IFantasyFootballStore, PostgresFantasyFootballStore>();
            return services;
        }
    }
}