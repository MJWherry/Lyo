using Lyo.Exceptions;
using Lyo.Geolocation.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Geolocation.Postgres;

/// <summary>Extension methods for PostgreSQL geolocation store registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds GeolocationDbContextFactory to the service collection.</summary>
        public IServiceCollection AddGeolocationDbContextFactory(Action<PostgresGeolocationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresGeolocationOptions();
            configure(options);
            return services.AddGeolocationDbContextFactory(options);
        }

        /// <summary>Adds GeolocationDbContextFactory using configuration binding.</summary>
        public IServiceCollection AddGeolocationDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresGeolocationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresGeolocationOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddGeolocationDbContextFactory(options);
        }

        /// <summary>Adds GeolocationDbContextFactory to the service collection.</summary>
        public IServiceCollection AddGeolocationDbContextFactory(PostgresGeolocationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<GeolocationDbContext, PostgresGeolocationOptions>();
            services.AddDbContextFactory<GeolocationDbContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresGeolocationOptions.Schema)));

            return services;
        }

        /// <summary>Adds PostgreSQL geolocation store (DbContext factory and <see cref="IGeolocationStore" />).</summary>
        public IServiceCollection AddPostgresGeolocationStore(Action<PostgresGeolocationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresGeolocationOptions();
            configure(options);
            return services.AddPostgresGeolocationStore(options);
        }

        /// <summary>Adds PostgreSQL geolocation store using configuration binding.</summary>
        public IServiceCollection AddPostgresGeolocationStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresGeolocationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresGeolocationOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresGeolocationStore(options);
        }

        /// <summary>Adds PostgreSQL geolocation store to the service collection.</summary>
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