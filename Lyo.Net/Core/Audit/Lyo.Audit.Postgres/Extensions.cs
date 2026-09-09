using Lyo.Audit.Postgres.Database;
using Lyo.Configuration;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Audit.Postgres;

/// <summary>DI helpers that register the PostgreSQL audit context.</summary>
public static class Extensions
{
    /// <param name="services">Collection to add services to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="AuditDbContext" /> on the collection.</summary>
        /// <param name="connectionString">PostgreSQL connection string</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddAuditDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddPostgresAuditRecorder(new PostgresAuditOptions { ConnectionString = connectionString })
                .AddScoped<AuditDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AuditDbContext>>().CreateDbContext());
        }

        /// <summary>Registers an audit <see cref="IDbContextFactory{TContext}" /> only (no recorder).</summary>
        /// <param name="configure">Callback that fills <see cref="PostgresAuditOptions" /></param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddAuditDbContextFactory(Action<PostgresAuditOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAuditOptions();
            configure(options);
            return services.AddAuditDbContextFactory(options);
        }

        /// <summary>Registers an audit <see cref="IDbContextFactory{TContext}" /> from configuration.</summary>
        /// <param name="configuration">Root configuration (for example <c>builder.Configuration</c>)</param>
        /// <param name="configSectionName">Section to bind; defaults to <see cref="PostgresAuditOptions.SectionName" /></param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddAuditDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresAuditOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresAuditOptions>(configuration, configSectionName);

            return services.AddAuditDbContextFactory(options);
        }

        /// <summary>Registers an audit <see cref="IDbContextFactory{TContext}" /> only (no recorder).</summary>
        /// <param name="options">PostgreSQL audit settings</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddAuditDbContextFactory(PostgresAuditOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<AuditDbContext, PostgresAuditOptions>(options);

            return services;
        }

        /// <summary>Registers the audit factory plus <see cref="PostgresAuditRecorder" /> as <see cref="IAuditRecorder" />.</summary>
        /// <param name="configure">Callback that fills <see cref="PostgresAuditOptions" /></param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddPostgresAuditRecorder(Action<PostgresAuditOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAuditOptions();
            configure(options);
            return services.AddPostgresAuditRecorder(options);
        }

        /// <summary>Registers the audit factory and <see cref="PostgresAuditRecorder" /> from configuration.</summary>
        /// <param name="configuration">Root configuration (for example <c>builder.Configuration</c>)</param>
        /// <param name="configSectionName">Section to bind; defaults to <see cref="PostgresAuditOptions.SectionName" /></param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddPostgresAuditRecorderFromConfiguration(IConfiguration configuration, string configSectionName = PostgresAuditOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresAuditOptions>(configuration, configSectionName);

            return services.AddPostgresAuditRecorder(options);
        }

        /// <summary>Registers the audit factory and <see cref="PostgresAuditRecorder" />.</summary>
        /// <param name="options">PostgreSQL audit settings</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddPostgresAuditRecorder(PostgresAuditOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddAuditDbContextFactory(options);
            services.AddEntityRefOptions();
            services.AddSingleton<IAuditRecorder, PostgresAuditRecorder>();
            return services;
        }
    }
}