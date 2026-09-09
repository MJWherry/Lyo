using Lyo.Configuration;
using Lyo.ContactUs.Models;
using Lyo.ContactUs.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.ContactUs.Postgres;

/// <summary>DI helpers for PostgreSQL contact form service registration.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers ContactUsDbContext to the service collection.</summary>
        /// <param name="connectionString">Postgres connection string</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddContactUsDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddContactUsDbContextFactory(new PostgresContactUsOptions { ConnectionString = connectionString })
                .AddScoped<ContactUsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ContactUsDbContext>>().CreateDbContext());
        }

        /// <summary>Registers PostgreSQL contact form DbContextFactory to the service collection.</summary>
        /// <param name="configure">Callback that fills Postgres contact-form options</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddContactUsDbContextFactory(Action<PostgresContactUsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresContactUsOptions();
            configure(options);
            return services.AddContactUsDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL contact form DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">Host configuration (for example builder.Configuration)</param>
        /// <param name="configSectionName">Configuration section name; defaults to PostgresContactUsOptions.SectionName</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddContactUsDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresContactUsOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresContactUsOptions>(configuration, configSectionName);

            return services.AddContactUsDbContextFactory(options);
        }

        /// <summary>Registers PostgreSQL contact form DbContextFactory to the service collection.</summary>
        /// <param name="options">Postgres contact-form options</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddContactUsDbContextFactory(PostgresContactUsOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<ContactUsDbContext, PostgresContactUsOptions>(options);

            return services;
        }

        /// <summary>Registers PostgreSQL contact form DbContextFactory and service to the service collection.</summary>
        /// <param name="configure">Callback that fills Postgres contact-form options</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddContactUsPostgres(Action<PostgresContactUsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresContactUsOptions();
            configure(options);
            return services.AddContactUsPostgres(options);
        }

        /// <summary>Registers PostgreSQL contact form DbContextFactory and service using configuration binding.</summary>
        /// <param name="configuration">Host configuration (for example builder.Configuration)</param>
        /// <param name="configSectionName">Configuration section name; defaults to PostgresContactUsOptions.SectionName</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddContactUsPostgresFromConfiguration(IConfiguration configuration, string configSectionName = PostgresContactUsOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = LyoOptions.Bind<PostgresContactUsOptions>(configuration, configSectionName);

            return services.AddContactUsPostgres(options);
        }

        /// <summary>Registers PostgreSQL contact form DbContextFactory and service to the service collection.</summary>
        /// <param name="options">Postgres contact-form options</param>
        /// <returns>Same collection so registration can be chained</returns>
        public IServiceCollection AddContactUsPostgres(PostgresContactUsOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddContactUsDbContextFactory(options);
            if (!services.Any(s => s.ServiceType == typeof(ContactUsServiceOptions)))
                services.AddSingleton(new ContactUsServiceOptions());

            services.AddScoped<IContactUsService, PostgresContactUsService>();
            return services;
        }
    }
}