using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Rating.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Rating.Postgres;

/// <summary>Extension methods for PostgreSQL rating store registration.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds PostgreSQL rating DbContextFactory to the service collection (IDbContextFactory only).</summary>
        public IServiceCollection AddRatingDbContextFactory(Action<PostgresRatingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresRatingOptions();
            configure(options);
            return services.AddRatingDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL rating DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddRatingDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresRatingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresRatingOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddRatingDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL rating DbContextFactory to the service collection.</summary>
        public IServiceCollection AddRatingDbContextFactory(PostgresRatingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddOptions<EntityRefOptions>();
            services.AddPostgresMigrations<RatingDbContext, PostgresRatingOptions>();
            services.AddDbContextFactory<RatingDbContext>(dbOpts => dbOpts.UseNpgsql(
                options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresRatingOptions.Schema)));

            return services;
        }

        /// <summary>Adds PostgreSQL rating DbContextFactory and PostgresRatingStore (IRatingStore) to the service collection.</summary>
        public IServiceCollection AddPostgresRatingStore(Action<PostgresRatingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresRatingOptions();
            configure(options);
            return services.AddPostgresRatingStore(options);
        }

        /// <summary>Adds PostgreSQL rating store using configuration binding.</summary>
        public IServiceCollection AddPostgresRatingStoreFromConfiguration(IConfiguration configuration, string configSectionName = PostgresRatingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresRatingOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresRatingStore(options);
        }

        /// <summary>Adds PostgreSQL rating DbContextFactory and PostgresRatingStore to the service collection.</summary>
        public IServiceCollection AddPostgresRatingStore(PostgresRatingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddRatingDbContextFactory(options);
            services.AddSingleton<IRatingStore>(
                sp => new PostgresRatingStore(
                    sp.GetRequiredService<IDbContextFactory<RatingDbContext>>(),
                    sp.GetRequiredService<IOptions<EntityRefOptions>>(),
                    sp.GetServices<IEntityRefActionInterceptor>()));
            return services;
        }
    }
}