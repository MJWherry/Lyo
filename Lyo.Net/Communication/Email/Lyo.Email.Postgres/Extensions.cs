using Lyo.Configuration;
using Lyo.Email.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Email.Postgres;

/// <summary>DI helpers for PostgreSQL email logging.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers EmailDbContext on the collection.</summary>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddEmailDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddEmailDbContextFactory(new PostgresEmailOptions { ConnectionString = connectionString })
                .AddScoped<EmailDbContext>(sp => sp.GetRequiredService<IDbContextFactory<EmailDbContext>>().CreateDbContext());
        }

        /// <summary>Registers a PostgreSQL email DbContextFactory and schema. Callers map and insert from EmailSent events.</summary>
        /// <param name="configure">Callback that mutates PostgreSQL email-log options.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddEmailDbContextFactory(Action<PostgresEmailOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresEmailOptions();
            configure(options);
            return services.AddEmailDbContextFactory(options);
        }

        /// <summary>Registers a PostgreSQL email DbContextFactory by binding options from configuration.</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration).</param>
        /// <param name="configSectionName">Section to bind (defaults to PostgresEmailOptions.SectionName).</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddEmailDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresEmailOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresEmailOptions>(configuration, configSectionName);

            return services.AddEmailDbContextFactory(options);
        }

        /// <summary>Registers a PostgreSQL email DbContextFactory and schema. Callers map and insert from EmailSent events.</summary>
        /// <param name="options">PostgreSQL email-log options to use.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddEmailDbContextFactory(PostgresEmailOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<EmailDbContext, PostgresEmailOptions>(options);

            return services;
        }
    }
}