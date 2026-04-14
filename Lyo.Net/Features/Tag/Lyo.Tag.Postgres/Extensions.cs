using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Tag.Postgres;

/// <summary>Extension methods for PostgreSQL tag store registration.</summary>
public static class Extensions
{
    /// <summary>Adds PostgreSQL tag DbContextFactory to the service collection (IDbContextFactory only).</summary>
    public static IServiceCollection AddTagDbContextFactory(this IServiceCollection services, Action<PostgresTagOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresTagOptions();
        configure(options);
        return services.AddTagDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL tag DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddTagDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresTagOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresTagOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddTagDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL tag DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddTagDbContextFactory(this IServiceCollection services, PostgresTagOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresTagOptions>>(Options.Create(options));
        services.AddPostgresMigrations<TagDbContext, PostgresTagOptions>();
        services.AddDbContextFactory<TagDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresTagOptions.Schema)));

        return services;
    }

    /// <summary>Adds PostgreSQL tag DbContextFactory and PostgresTagStore (ITagStore) to the service collection.</summary>
    public static IServiceCollection AddPostgresTagStore(this IServiceCollection services, Action<PostgresTagOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresTagOptions();
        configure(options);
        return services.AddPostgresTagStore(options);
    }

    /// <summary>Adds PostgreSQL tag store using configuration binding.</summary>
    public static IServiceCollection AddPostgresTagStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresTagOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresTagOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresTagStore(options);
    }

    /// <summary>Adds PostgreSQL tag DbContextFactory and PostgresTagStore to the service collection.</summary>
    public static IServiceCollection AddPostgresTagStore(this IServiceCollection services, PostgresTagOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddTagDbContextFactory(options);
        services.AddSingleton<ITagStore, PostgresTagStore>();
        return services;
    }
}