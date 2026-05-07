using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Web.Reporting.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Web.Reporting.Postgres;

/// <summary>Extension methods for PostgreSQL reporting service registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds ReportingDbContext to the service collection.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddReportingDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddReportingDbContextFactory(new PostgresReportingOptions { ConnectionString = connectionString })
                .AddScoped<ReportingDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ReportingDbContext>>().CreateDbContext());
        }

        /// <summary>Adds ReportingDbContext to the service collection.</summary>
        /// <param name="configure">Action to configure the DbContext options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddReportingDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<ReportingDbContext>(configure);
            return services;
        }

        /// <summary>Adds PostgreSQL reporting DbContextFactory to the service collection.</summary>
        /// <param name="configure">Action to configure the PostgreSQL reporting options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddReportingDbContextFactory(Action<PostgresReportingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresReportingOptions();
            configure(options);
            return services.AddReportingDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL reporting DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresReportingOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddReportingDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresReportingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresReportingOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddReportingDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL reporting DbContextFactory to the service collection.</summary>
        /// <param name="options">The PostgreSQL reporting options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddReportingDbContextFactory(PostgresReportingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<ReportingDbContext, PostgresReportingOptions>();
            services.AddDbContextFactory<ReportingDbContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresReportingOptions.Schema)));

            return services;
        }
    }
}