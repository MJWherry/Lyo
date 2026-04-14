using Lyo.Config.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Config.Postgres;

/// <summary>Extension methods for PostgreSQL config store registration.</summary>
public static class Extensions
{
    /// <summary>Adds PostgreSQL config DbContextFactory to the service collection (IDbContextFactory only).</summary>
    public static IServiceCollection AddConfigDbContextFactory(this IServiceCollection services, Action<PostgresConfigOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresConfigOptions();
        configure(options);
        return services.AddConfigDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL config DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddConfigDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresConfigOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresConfigOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddConfigDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL config DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddConfigDbContextFactory(this IServiceCollection services, PostgresConfigOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresConfigOptions>>(Options.Create(options));
        services.AddPostgresMigrations<ConfigDbContext, PostgresConfigOptions>();
        services.AddDbContextFactory<ConfigDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresConfigOptions.Schema)));

        return services;
    }

    /// <summary>Adds PostgreSQL config store registration using configuration.</summary>
    public static IServiceCollection AddPostgresConfigStore(this IServiceCollection services, Action<PostgresConfigOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresConfigOptions();
        configure(options);
        return services.AddPostgresConfigStore(options);
    }

    /// <summary>Adds PostgreSQL config store registration using configuration binding.</summary>
    public static IServiceCollection AddPostgresConfigStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresConfigOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresConfigOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresConfigStore(options);
    }

    /// <summary>Adds PostgreSQL config store registration.</summary>
    public static IServiceCollection AddPostgresConfigStore(this IServiceCollection services, PostgresConfigOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddConfigDbContextFactory(options);
        services.AddSingleton<IConfigStore, PostgresConfigStore>();
        return services;
    }
}