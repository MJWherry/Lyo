using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Geolocation.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Geolocation.Postgres;

/// <summary>Helpers that register the PostgreSQL geolocation store.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers a GeolocationDbContextFactory from a configure delegate.</summary>
        public IServiceCollection AddGeolocationDbContextFactory(Action<PostgresGeolocationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresGeolocationOptions();
            configure(options);
            return services.AddGeolocationDbContextFactory(options);
        }

        /// <summary>Registers a GeolocationDbContextFactory from configuration.</summary>
        public IServiceCollection AddGeolocationDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresGeolocationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresGeolocationOptions>(configuration, configSectionName);

            return services.AddGeolocationDbContextFactory(options);
        }

        /// <summary>Registers a GeolocationDbContextFactory from options.</summary>
        public IServiceCollection AddGeolocationDbContextFactory(PostgresGeolocationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<GeolocationDbContext, PostgresGeolocationOptions>(options);

            return services;
        }

        /// <summary>Registers the PostgreSQL geolocation store (DbContext factory and <see cref="IGeolocationStore" />).</summary>
        public IServiceCollection AddPostgresGeolocationStore(Action<PostgresGeolocationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresGeolocationOptions();
            configure(options);
            return services.AddPostgresGeolocationStore(options);
        }

        /// <summary>Registers the PostgreSQL geolocation store from configuration.</summary>
        public IServiceCollection AddPostgresGeolocationStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresGeolocationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresGeolocationOptions>(configuration, configSectionName);

            return services.AddPostgresGeolocationStore(options);
        }

        /// <summary>Registers the PostgreSQL geolocation store from options.</summary>
        public IServiceCollection AddPostgresGeolocationStore(PostgresGeolocationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddGeolocationDbContextFactory(options);
            services.AddSingleton<IGeolocationStore, PostgresGeolocationStore>();
            return services;
        }
    }
}