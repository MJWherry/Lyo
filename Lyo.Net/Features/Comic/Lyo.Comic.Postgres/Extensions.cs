using Lyo.Comic.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Comic.Postgres;

/// <summary>Extension methods for registering the PostgreSQL comic store.</summary>
public static class Extensions
{
    /// <summary>Adds the PostgreSQL comic DbContextFactory using explicit options.</summary>
    public static IServiceCollection AddComicDbContextFactory(this IServiceCollection services, Action<PostgresComicOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresComicOptions();
        configure(options);
        return services.AddComicDbContextFactory(options);
    }

    /// <summary>Adds the PostgreSQL comic DbContextFactory by binding from configuration.</summary>
    public static IServiceCollection AddComicDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresComicOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresComicOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddComicDbContextFactory(options);
    }

    /// <summary>Adds the PostgreSQL comic DbContextFactory using a pre-built options instance.</summary>
    public static IServiceCollection AddComicDbContextFactory(this IServiceCollection services, PostgresComicOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresComicOptions>>(Options.Create(options));
        services.AddPostgresMigrations<ComicDbContext, PostgresComicOptions>();
        services.AddDbContextFactory<ComicDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresComicOptions.Schema)));

        return services;
    }

    /// <summary>Adds the PostgreSQL comic DbContextFactory and <see cref="IComicStore" /> using explicit options.</summary>
    public static IServiceCollection AddPostgresComicStore(this IServiceCollection services, Action<PostgresComicOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresComicOptions();
        configure(options);
        return services.AddPostgresComicStore(options);
    }

    /// <summary>Adds the PostgreSQL comic DbContextFactory and <see cref="IComicStore" /> by binding from configuration.</summary>
    public static IServiceCollection AddPostgresComicStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresComicOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresComicOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresComicStore(options);
    }

    /// <summary>Adds the PostgreSQL comic DbContextFactory and <see cref="IComicStore" /> using a pre-built options instance.</summary>
    public static IServiceCollection AddPostgresComicStore(this IServiceCollection services, PostgresComicOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddComicDbContextFactory(options);
        services.AddSingleton<IComicStore, PostgresComicStore>();
        return services;
    }
}
