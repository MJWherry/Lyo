using Lyo.Exceptions;
using Lyo.HomeInventory.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.HomeInventory.Postgres;

/// <summary>DI registration for PostgreSQL home-inventory storage.</summary>
public static class Extensions
{
    public static IServiceCollection AddHomeInventoryDbContextFactory(this IServiceCollection services, Action<PostgresHomeInventoryOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresHomeInventoryOptions();
        configure(options);
        return services.AddHomeInventoryDbContextFactory(options);
    }

    public static IServiceCollection AddHomeInventoryDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresHomeInventoryOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresHomeInventoryOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddHomeInventoryDbContextFactory(options);
    }

    public static IServiceCollection AddHomeInventoryDbContextFactory(this IServiceCollection services, PostgresHomeInventoryOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresHomeInventoryOptions>>(Options.Create(options));
        services.AddPostgresMigrations<HomeInventoryDbContext, PostgresHomeInventoryOptions>();
        services.AddDbContextFactory<HomeInventoryDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresHomeInventoryOptions.Schema)));

        return services;
    }

    public static IServiceCollection AddPostgresHomeInventoryStore(this IServiceCollection services, Action<PostgresHomeInventoryOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresHomeInventoryOptions();
        configure(options);
        return services.AddPostgresHomeInventoryStore(options);
    }

    public static IServiceCollection AddPostgresHomeInventoryStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresHomeInventoryOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresHomeInventoryOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresHomeInventoryStore(options);
    }

    public static IServiceCollection AddPostgresHomeInventoryStore(this IServiceCollection services, PostgresHomeInventoryOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddHomeInventoryDbContextFactory(options);
        services.AddSingleton<IHomeInventoryStore, PostgresHomeInventoryStore>();
        return services;
    }
}