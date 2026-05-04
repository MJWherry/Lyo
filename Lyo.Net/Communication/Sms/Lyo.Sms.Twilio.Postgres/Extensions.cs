using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Sms.Twilio.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Sms.Twilio.Postgres;

/// <summary>Extension methods for PostgreSQL Twilio SMS logging database context registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds TwilioSmsDbContext to the service collection.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwilioSmsDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddTwilioSmsDbContextFactory(new PostgresTwilioSmsOptions { ConnectionString = connectionString })
                .AddScoped<TwilioSmsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<TwilioSmsDbContext>>().CreateDbContext());
        }

        /// <summary>Adds PostgreSQL Twilio SMS DbContextFactory to the service collection.</summary>
        public IServiceCollection AddTwilioSmsDbContextFactory(Action<PostgresTwilioSmsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresTwilioSmsOptions();
            configure(options);
            return services.AddTwilioSmsDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL Twilio SMS DbContextFactory using configuration binding.</summary>
        public IServiceCollection AddTwilioSmsDbContextFactoryFromConfiguration(
            IConfiguration configuration,
            string configSectionName = PostgresTwilioSmsOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresTwilioSmsOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddTwilioSmsDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL Twilio SMS DbContextFactory to the service collection.</summary>
        public IServiceCollection AddTwilioSmsDbContextFactory(PostgresTwilioSmsOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString);
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<TwilioSmsDbContext, PostgresTwilioSmsOptions>();
            services.AddDbContextFactory<TwilioSmsDbContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresTwilioSmsOptions.Schema)));

            return services;
        }

        /// <summary>Adds TwilioSmsDbContext to the service collection.</summary>
        /// <param name="configure">Action to configure the DbContext options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddTwilioSmsDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<TwilioSmsDbContext>(configure);
            return services;
        }
    }
}