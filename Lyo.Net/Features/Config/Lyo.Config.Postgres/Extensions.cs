using Lyo.Config.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Config.Postgres;

/// <summary>Extension methods for PostgreSQL config store registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds PostgreSQL config DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddConfigDbContextFactory(Action<PostgresConfigOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresConfigOptions();
            configure(options);
            return services.AddConfigDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL config DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddConfigDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresConfigOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresConfigOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddConfigDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL config DbContextFactory to the service collection.</summary>
        public IServiceCollection AddConfigDbContextFactory(PostgresConfigOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<ConfigDbContext, PostgresConfigOptions>();
            services.AddDbContextFactory<ConfigDbContext>(dbOpts => dbOpts.UseNpgsql(
                options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresConfigOptions.Schema)));

            return services;
        }

        /// <summary>Adds PostgreSQL config store registration using configuration.</summary>
        public IServiceCollection AddPostgresConfigStore(Action<PostgresConfigOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresConfigOptions();
            configure(options);
            return services.AddPostgresConfigStore(options);
        }

        /// <summary>Adds PostgreSQL config store registration using configuration binding.</summary>
        public IServiceCollection AddPostgresConfigStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresConfigOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresConfigOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresConfigStore(options);
        }

        /// <summary>Adds PostgreSQL config store registration.</summary>
        public IServiceCollection AddPostgresConfigStore(PostgresConfigOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddConfigDbContextFactory(options);
            services.AddSingleton<IConfigStore, PostgresConfigStore>();
            return services;
        }
    }
}