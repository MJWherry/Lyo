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
    /// <summary>Adds ContactUsDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContactUsDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddContactUsDbContextFactory(new PostgresContactUsOptions { ConnectionString = connectionString })
            .AddScoped<ContactUsDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ContactUsDbContext>>().CreateDbContext());
    }

    /// <summary>Adds PostgreSQL contact form DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL contact form options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContactUsDbContextFactory(this IServiceCollection services, Action<PostgresContactUsOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresContactUsOptions();
        configure(options);
        return services.AddContactUsDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL contact form DbContextFactory to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresContactUsOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContactUsDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresContactUsOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresContactUsOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddContactUsDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL contact form DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL contact form options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContactUsDbContextFactory(this IServiceCollection services, PostgresContactUsOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresContactUsOptions>>(Options.Create(options));
        services.AddPostgresMigrations<ContactUsDbContext, PostgresContactUsOptions>();
        services.AddDbContextFactory<ContactUsDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresContactUsOptions.Schema)));

        return services;
    }

    /// <summary>Adds ContactUsDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContactUsDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<ContactUsDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL contact form DbContextFactory and service to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL contact form options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContactUsPostgres(this IServiceCollection services, Action<PostgresContactUsOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresContactUsOptions();
        configure(options);
        return services.AddContactUsPostgres(options);
    }

    /// <summary>Adds PostgreSQL contact form DbContextFactory and service using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresContactUsOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContactUsPostgresFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresContactUsOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresContactUsOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddContactUsPostgres(options);
    }

    /// <summary>Adds PostgreSQL contact form DbContextFactory and service to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL contact form options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddContactUsPostgres(this IServiceCollection services, PostgresContactUsOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddContactUsDbContextFactory(options);
        if (!services.Any(s => s.ServiceType == typeof(ContactUsServiceOptions)))
            services.AddSingleton(new ContactUsServiceOptions());

        services.AddScoped<IContactUsService, PostgresContactUsService>();
        return services;
    }
}