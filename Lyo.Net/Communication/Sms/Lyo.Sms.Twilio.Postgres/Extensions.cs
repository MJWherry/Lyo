using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Sms.Twilio.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Sms.Twilio.Postgres;

/// <summary>DI helpers that register the PostgreSQL Twilio SMS logging context.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers TwilioSmsDbContext on the collection.</summary>
        /// <param name="connectionString">PostgreSQL connection string</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddTwilioSmsDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddTwilioSmsDbContextFactory(new PostgresTwilioSmsOptions { ConnectionString = connectionString })
                .AddScoped<TwilioSmsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<TwilioSmsDbContext>>().CreateDbContext());
        }

        /// <summary>Registers a PostgreSQL Twilio SMS DbContextFactory.</summary>
        public IServiceCollection AddTwilioSmsDbContextFactory(Action<PostgresTwilioSmsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresTwilioSmsOptions();
            configure(options);
            return services.AddTwilioSmsDbContextFactory(options);
        }

        /// <summary>Registers a PostgreSQL Twilio SMS DbContextFactory by binding options from configuration.</summary>
        public IServiceCollection AddTwilioSmsDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresTwilioSmsOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresTwilioSmsOptions();
            configuration.GetSection(configSectionName).Bind(options);

            return services.AddTwilioSmsDbContextFactory(options);
        }

        /// <summary>Registers a PostgreSQL Twilio SMS DbContextFactory on the collection.</summary>
        public IServiceCollection AddTwilioSmsDbContextFactory(PostgresTwilioSmsOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<TwilioSmsDbContext, PostgresTwilioSmsOptions>(options);

            return services;
        }
    }
}