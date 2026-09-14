using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Validation.Models;
using Lyo.Validation.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Lyo.Validation.Postgres;

/// <summary>Helpers that register PostgreSQL validation schema storage.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers a <see cref="ValidationDbContext" /> factory from a configure delegate.</summary>
        public IServiceCollection AddValidationDbContextFactory(Action<PostgresValidationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresValidationOptions();
            configure(options);
            return services.AddValidationDbContextFactory(options);
        }

        /// <summary>Registers a <see cref="ValidationDbContext" /> factory from configuration.</summary>
        public IServiceCollection AddValidationDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresValidationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresValidationOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddValidationDbContextFactory(options);
        }

        /// <summary>Registers a <see cref="ValidationDbContext" /> factory from options.</summary>
        public IServiceCollection AddValidationDbContextFactory(PostgresValidationOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<ValidationDbContext, PostgresValidationOptions>(options);
            return services;
        }

        /// <summary>Registers the PostgreSQL validation schema store from a configure delegate.</summary>
        public IServiceCollection AddPostgresValidationStore(Action<PostgresValidationOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresValidationOptions();
            configure(options);
            return services.AddPostgresValidationStore(options);
        }

        /// <summary>Registers the PostgreSQL validation schema store from configuration.</summary>
        public IServiceCollection AddPostgresValidationStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresValidationOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresValidationOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddPostgresValidationStore(options);
        }

        /// <summary>Registers the PostgreSQL validation schema store from options.</summary>
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
