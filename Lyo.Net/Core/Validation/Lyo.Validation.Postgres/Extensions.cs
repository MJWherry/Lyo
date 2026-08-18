using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Validation.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Validation.Postgres;

/// <summary>DI registration for PostgreSQL validation schema storage.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="ValidationDbContext" /> factory from a configure delegate.</summary>
        public IServiceCollection AddValidationDbContextFactory(Action<PostgresValidationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresValidationOptions();
            configure(options);
            return services.AddValidationDbContextFactory(options);
        }

        /// <summary>Adds <see cref="ValidationDbContext" /> factory from configuration.</summary>
        public IServiceCollection AddValidationDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresValidationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresValidationOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddValidationDbContextFactory(options);
        }

        /// <summary>Adds <see cref="ValidationDbContext" /> factory from options.</summary>
        public IServiceCollection AddValidationDbContextFactory(PostgresValidationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<ValidationDbContext, PostgresValidationOptions>();
            services.AddDbContextFactory<ValidationDbContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresValidationOptions.Schema)));
            return services;
        }

        /// <summary>Adds PostgreSQL validation schema store from a configure delegate.</summary>
        public IServiceCollection AddPostgresValidationStore(Action<PostgresValidationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresValidationOptions();
            configure(options);
            return services.AddPostgresValidationStore(options);
        }

        /// <summary>Adds PostgreSQL validation schema store from configuration.</summary>
        public IServiceCollection AddPostgresValidationStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresValidationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresValidationOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresValidationStore(options);
        }

        /// <summary>Adds PostgreSQL validation schema store from options.</summary>
        public IServiceCollection AddPostgresValidationStore(PostgresValidationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddValidationDbContextFactory(options);
            services.TryAddSingleton<IValidationSchemaStore, PostgresValidationSchemaStore>();
            return services;
        }
    }
}
