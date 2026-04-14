using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.ShortUrl.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.ShortUrl.Postgres;

/// <summary>Extension methods for PostgreSQL URL shortener database context registration.</summary>
public static class Extensions
{
    /// <summary>Adds ShortUrlDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddShortUrlDbContext(this IServiceCollection services, string connectionString)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        return services.AddShortUrlDbContextFactory(new PostgresShortUrlOptions { ConnectionString = connectionString })
            .AddScoped<ShortUrlDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ShortUrlDbContext>>().CreateDbContext());
    }

    /// <summary>Adds ShortUrlDbContext to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the DbContext options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddShortUrlDbContext(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        services.AddDbContext<ShortUrlDbContext>(configure);
        return services;
    }

    /// <summary>Adds PostgreSQL URL shortener DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the PostgreSQL URL shortener options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddShortUrlDbContextFactory(this IServiceCollection services, Action<PostgresShortUrlOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresShortUrlOptions();
        configure(options);
        return services.AddShortUrlDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL URL shortener DbContextFactory to the service collection using configuration binding.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration)</param>
    /// <param name="configSectionName">The configuration section name (defaults to PostgresShortUrlOptions.SectionName)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddShortUrlDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresShortUrlOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresShortUrlOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddShortUrlDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL URL shortener DbContextFactory to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="options">The PostgreSQL URL shortener options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddShortUrlDbContextFactory(this IServiceCollection services, PostgresShortUrlOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresShortUrlOptions>>(Options.Create(options));
        services.AddPostgresMigrations<ShortUrlDbContext, PostgresShortUrlOptions>();
        services.AddDbContextFactory<ShortUrlDbContext>(dbOptions => dbOptions.UseNpgsql(
            options.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresShortUrlOptions.Schema)));

        return services;
    }
}