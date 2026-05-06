using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Tag.Postgres;

/// <summary>Extension methods for PostgreSQL tag store registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds PostgreSQL tag DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddTagDbContextFactory(Action<PostgresTagOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresTagOptions();
            configure(options);
            return services.AddTagDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL tag DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddTagDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresTagOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresTagOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddTagDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL tag DbContextFactory to the service collection.</summary>
        public IServiceCollection AddTagDbContextFactory(PostgresTagOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<TagDbContext, PostgresTagOptions>();
            services.AddDbContextFactory<TagDbContext>(dbOpts => dbOpts.UseNpgsql(
                options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresTagOptions.Schema)));

            return services;
        }

        /// <summary>Adds PostgreSQL tag DbContextFactory and PostgresTagStore (ITagStore) to the service collection.</summary>
        public IServiceCollection AddPostgresTagStore(Action<PostgresTagOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresTagOptions();
            configure(options);
            return services.AddPostgresTagStore(options);
        }

        /// <summary>Adds PostgreSQL tag store using configuration binding.</summary>
        public IServiceCollection AddPostgresTagStoreFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresTagOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresTagOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresTagStore(options);
        }

        /// <summary>Adds PostgreSQL tag DbContextFactory and PostgresTagStore to the service collection.</summary>
        public IServiceCollection AddPostgresTagStore(PostgresTagOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddTagDbContextFactory(options);
            services.AddSingleton<ITagStore, PostgresTagStore>();
            return services;
        }
    }
}