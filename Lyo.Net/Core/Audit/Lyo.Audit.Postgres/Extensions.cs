using Lyo.Audit.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Audit.Postgres;

/// <summary>Extension methods for PostgreSQL audit database context registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds AuditDbContext to the service collection.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAuditDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddPostgresAuditRecorder(new PostgresAuditOptions { ConnectionString = connectionString })
                .AddScoped<AuditDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AuditDbContext>>().CreateDbContext());
        }

        /// <summary>Adds AuditDbContext to the service collection.</summary>
        /// <param name="configure">Action to configure the DbContext options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAuditDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<AuditDbContext>(configure);
            return services;
        }

        /// <summary>Adds PostgreSQL audit DbContextFactory to the service collection (IDbContextFactory only).</summary>
        /// <param name="configure">Action to configure the PostgreSQL audit options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAuditDbContextFactory(Action<PostgresAuditOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAuditOptions();
            configure(options);
            return services.AddAuditDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL audit DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresAuditOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAuditDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresAuditOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresAuditOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddAuditDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL audit DbContextFactory to the service collection (IDbContextFactory only).</summary>
        /// <param name="options">The PostgreSQL audit options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddAuditDbContextFactory(PostgresAuditOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<AuditDbContext, PostgresAuditOptions>();
            services.AddDbContextFactory<AuditDbContext>(dbOpts => dbOpts.UseNpgsql(
                options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresAuditOptions.Schema)));

            return services;
        }

        /// <summary>Adds PostgreSQL audit DbContextFactory and PostgresAuditRecorder (IAuditRecorder) to the service collection.</summary>
        /// <param name="configure">Action to configure the PostgreSQL audit options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPostgresAuditRecorder(Action<PostgresAuditOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAuditOptions();
            configure(options);
            return services.AddPostgresAuditRecorder(options);
        }

        /// <summary>Adds PostgreSQL audit DbContextFactory and PostgresAuditRecorder to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresAuditOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPostgresAuditRecorderFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresAuditOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresAuditOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddPostgresAuditRecorder(options);
        }

        /// <summary>Adds PostgreSQL audit DbContextFactory and PostgresAuditRecorder to the service collection.</summary>
        /// <param name="options">The PostgreSQL audit options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddPostgresAuditRecorder(PostgresAuditOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddAuditDbContextFactory(options);
            services.AddSingleton<IAuditRecorder, PostgresAuditRecorder>();
            return services;
        }
    }
}