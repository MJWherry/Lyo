using Lyo.ContactUs.Models;
using Lyo.ContactUs.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.ContactUs.Postgres;

/// <summary>Extension methods for PostgreSQL contact form service registration.</summary>
public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds ContactUsDbContext to the service collection.</summary>
        /// <param name="connectionString">The PostgreSQL connection string</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddContactUsDbContext(string connectionString)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
            return services.AddContactUsDbContextFactory(new PostgresContactUsOptions { ConnectionString = connectionString })
                .AddScoped<ContactUsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ContactUsDbContext>>().CreateDbContext());
        }

        /// <summary>Adds PostgreSQL contact form DbContextFactory to the service collection.</summary>
        /// <param name="configure">Action to configure the PostgreSQL contact form options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddContactUsDbContextFactory(Action<PostgresContactUsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresContactUsOptions();
            configure(options);
            return services.AddContactUsDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL contact form DbContextFactory to the service collection using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresContactUsOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddContactUsDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresContactUsOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresContactUsOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddContactUsDbContextFactory(options);
        }

        /// <summary>Adds PostgreSQL contact form DbContextFactory to the service collection.</summary>
        /// <param name="options">The PostgreSQL contact form options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddContactUsDbContextFactory(PostgresContactUsOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            services.AddSingleton(Options.Create(options));
            services.AddPostgresMigrations<ContactUsDbContext, PostgresContactUsOptions>();
            services.AddDbContextFactory<ContactUsDbContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresContactUsOptions.Schema)));

            return services;
        }

        /// <summary>Adds ContactUsDbContext to the service collection.</summary>
        /// <param name="configure">Action to configure the DbContext options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddContactUsDbContext(Action<DbContextOptionsBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddDbContext<ContactUsDbContext>(configure);
            return services;
        }

        /// <summary>Adds PostgreSQL contact form DbContextFactory and service to the service collection.</summary>
        /// <param name="configure">Action to configure the PostgreSQL contact form options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddContactUsPostgres(Action<PostgresContactUsOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresContactUsOptions();
            configure(options);
            return services.AddContactUsPostgres(options);
        }

        /// <summary>Adds PostgreSQL contact form DbContextFactory and service using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
        /// <param name="configSectionName">The configuration section name (defaults to PostgresContactUsOptions.SectionName)</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddContactUsPostgresFromConfiguration(IConfiguration configuration, string configSectionName = PostgresContactUsOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new PostgresContactUsOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddContactUsPostgres(options);
        }

        /// <summary>Adds PostgreSQL contact form DbContextFactory and service to the service collection.</summary>
        /// <param name="options">The PostgreSQL contact form options</param>
        /// <returns>The service collection for chaining</returns>
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