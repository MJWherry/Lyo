using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Sms.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Sms.Postgres;

/// <summary>DI helpers that register the PostgreSQL SMS logging context.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers SmsDbContext on the collection.</summary>
        /// <param name="connectionString">PostgreSQL connection string</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddSmsDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddSmsDbContextFactory(new PostgresSmsOptions { ConnectionString = connectionString })
                .AddScoped<SmsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SmsDbContext>>().CreateDbContext());
        }

        /// <summary>Registers a PostgreSQL SMS logging DbContextFactory.</summary>
        /// <param name="configure">Callback that mutates PostgreSQL SMS-log options</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddSmsDbContextFactory(Action<PostgresSmsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresSmsOptions();
            configure(options);
            return services.AddSmsDbContextFactory(options);
        }

        /// <summary>Registers a PostgreSQL SMS logging DbContextFactory by binding options from configuration.</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration)</param>
        /// <param name="configSectionName">Section to bind (defaults to PostgresSmsOptions.SectionName)</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddSmsDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresSmsOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresSmsOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddSmsDbContextFactory(options);
        }

        /// <summary>Registers a PostgreSQL SMS logging DbContextFactory with prepared options.</summary>
        /// <param name="options">PostgreSQL SMS-log options to use</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddSmsDbContextFactory(PostgresSmsOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<SmsDbContext, PostgresSmsOptions>(options);

            return services;
        }
    }
}