using Lyo.Endato.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Endato.Postgres;

/// <summary>Extension methods for PostgreSQL Endato database context registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds EndatoDbContext to the service collection.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEndatoDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddEndatoDbContextFactory(new PostgresEndatoOptions { ConnectionString = connectionString })
                .AddScoped<EndatoDbContext>(sp => sp.GetRequiredService<IDbContextFactory<EndatoDbContext>>().CreateDbContext());
        }

        /// <summary>Adds EndatoDbContext to the service collection.</summary>
        /// <param name="configure">Action to configure the DbContext options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEndatoDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<EndatoDbContext>(configure);
            return services;
        }

        /// <summary>Adds PostgreSQL Endato DbContextFactory to the service collection.</summary>
        /// <param name="configure">Action to configure the PostgreSQL Endato options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEndatoDbContextFactory(Action<PostgresEndatoOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresEndatoOptions();
            configure(options);
            return services.AddEndatoDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL Endato DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresEndatoOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEndatoDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresEndatoOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresEndatoOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddEndatoDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL Endato DbContextFactory to the service collection.</summary>
        /// <param name="options">The PostgreSQL Endato options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEndatoDbContextFactory(PostgresEndatoOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<EndatoDbContext, PostgresEndatoOptions>();
            services.AddDbContextFactory<EndatoDbContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresEndatoOptions.Schema)));

            return services;
        }
    }
}