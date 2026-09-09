using Lyo.Configuration;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Rating.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Rating.Postgres;

/// <summary>DI helpers for PostgreSQL rating store registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers PostgreSQL rating DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddRatingDbContextFactory(Action<PostgresRatingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresRatingOptions();
            configure(options);
            return services.AddRatingDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL rating DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddRatingDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresRatingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresRatingOptions>(configuration, configSectionName);

            return services.AddRatingDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL rating DbContextFactory to the service collection.</summary>
        public IServiceCollection AddRatingDbContextFactory(PostgresRatingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<RatingDbContext, PostgresRatingOptions>(options);
            services.AddEntityRefOptions();

            return services;
        }

        /// <summary>Registers PostgreSQL rating DbContextFactory and PostgresRatingStore (IRatingStore) to the service collection.</summary>
        public IServiceCollection AddPostgresRatingStore(Action<PostgresRatingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresRatingOptions();
            configure(options);
            return services.AddPostgresRatingStore(options);
        }

        /// <summary>Registers PostgreSQL rating store using configuration binding.</summary>
        public IServiceCollection AddPostgresRatingStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresRatingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresRatingOptions>(configuration, configSectionName);

            return services.AddPostgresRatingStore(options);
        }

        /// <summary>Registers PostgreSQL rating DbContextFactory and PostgresRatingStore to the service collection.</summary>
        public IServiceCollection AddPostgresRatingStore(PostgresRatingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddRatingDbContextFactory(options);
            services.AddSingleton<IRatingStore>(sp => new PostgresRatingStore(
                sp.GetRequiredService<IDbContextFactory<RatingDbContext>>(), sp.GetRequiredService<EntityRefOptions>(),
                sp.GetRequiredService<PostgresRatingOptions>(), sp.GetServices<IEntityRefActionInterceptor>()));

            return services;
        }
    }
}