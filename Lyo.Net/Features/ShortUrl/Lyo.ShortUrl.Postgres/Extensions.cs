using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.ShortUrl.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.ShortUrl.Postgres;

/// <summary>Extension methods for PostgreSQL URL shortener database context registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds ShortUrlDbContext to the service collection.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddShortUrlDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddShortUrlDbContextFactory(new PostgresShortUrlOptions { ConnectionString = connectionString })
                .AddScoped<ShortUrlDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ShortUrlDbContext>>().CreateDbContext());
        }

        /// <summary>Adds ShortUrlDbContext to the service collection.</summary>
        /// <param name="configure">Action to configure the DbContext options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddShortUrlDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<ShortUrlDbContext>(configure);
            return services;
        }

        /// <summary>Adds PostgreSQL URL shortener DbContextFactory to the service collection.</summary>
        /// <param name="configure">Action to configure the PostgreSQL URL shortener options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddShortUrlDbContextFactory(Action<PostgresShortUrlOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresShortUrlOptions();
            configure(options);
            return services.AddShortUrlDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL URL shortener DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresShortUrlOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddShortUrlDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresShortUrlOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresShortUrlOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddShortUrlDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL URL shortener DbContextFactory to the service collection.</summary>
        /// <param name="options">The PostgreSQL URL shortener options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddShortUrlDbContextFactory(PostgresShortUrlOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<ShortUrlDbContext, PostgresShortUrlOptions>();
            services.AddDbContextFactory<ShortUrlDbContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresShortUrlOptions.Schema)));

            return services;
        }
    }
}