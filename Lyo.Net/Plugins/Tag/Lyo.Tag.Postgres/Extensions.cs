using Lyo.Configuration;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Tag.Postgres;

/// <summary>DI helpers for PostgreSQL tag store registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers PostgreSQL tag DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddTagDbContextFactory(Action<PostgresTagOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresTagOptions();
            configure(options);
            return services.AddTagDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL tag DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddTagDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresTagOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresTagOptions>(configuration, configSectionName);

            return services.AddTagDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL tag DbContextFactory to the service collection.</summary>
        public IServiceCollection AddTagDbContextFactory(PostgresTagOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<TagDbContext, PostgresTagOptions>(options);
            services.AddEntityRefOptions();

            return services;
        }

        /// <summary>Registers PostgreSQL tag DbContextFactory and PostgresTagStore (ITagStore) to the service collection.</summary>
        public IServiceCollection AddPostgresTagStore(Action<PostgresTagOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresTagOptions();
            configure(options);
            return services.AddPostgresTagStore(options);
        }

        /// <summary>Registers PostgreSQL tag store using configuration binding.</summary>
        public IServiceCollection AddPostgresTagStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresTagOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresTagOptions>(configuration, configSectionName);

            return services.AddPostgresTagStore(options);
        }

        /// <summary>Registers PostgreSQL tag DbContextFactory and PostgresTagStore to the service collection.</summary>
        public IServiceCollection AddPostgresTagStore(PostgresTagOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddTagDbContextFactory(options);
            services.AddSingleton<ITagStore>(sp => new PostgresTagStore(
                sp.GetRequiredService<IDbContextFactory<TagDbContext>>(), sp.GetRequiredService<EntityRefOptions>(),
                sp.GetRequiredService<PostgresTagOptions>(), sp.GetServices<IEntityRefActionInterceptor>()));

            return services;
        }
    }
}