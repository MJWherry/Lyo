using Lyo.Endato.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Endato.Postgres;

/// <summary>Extension methods for PostgreSQL Endato database context registration.</summary>
public static class Extensions
{
    /// <summary>Adds EndatoDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEndatoDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddEndatoDbContextFactory(new PostgresEndatoOptions { ConnectionString = connectionString })
            .AddScoped<EndatoDbContext>(sp => sp.GetRequiredService<IDbContextFactory<EndatoDbContext>>().CreateDbContext());
    }

    /// <summary>Adds EndatoDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEndatoDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<EndatoDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL Endato DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL Endato options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEndatoDbContextFactory(this IServiceCollection services, Action<PostgresEndatoOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresEndatoOptions();
        configure(options);
        return services.AddEndatoDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL Endato DbContextFactory to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresEndatoOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEndatoDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresEndatoOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresEndatoOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddEndatoDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL Endato DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL Endato options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEndatoDbContextFactory(this IServiceCollection services, PostgresEndatoOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresEndatoOptions>>(Options.Create(options));
        services.AddPostgresMigrations<EndatoDbContext, PostgresEndatoOptions>();
        services.AddDbContextFactory<EndatoDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresEndatoOptions.Schema)));

        return services;
    }
}