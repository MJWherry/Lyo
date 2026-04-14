using Lyo.Comment.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Comment.Postgres;

/// <summary>Extension methods for PostgreSQL comment store registration.</summary>
public static class Extensions
{
    /// <summary>Adds PostgreSQL comment DbContextFactory to the service collection (IDbContextFactory only).</summary>
    public static IServiceCollection AddCommentDbContextFactory(this IServiceCollection services, Action<PostgresCommentOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresCommentOptions();
        configure(options);
        return services.AddCommentDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL comment DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddCommentDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresCommentOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresCommentOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddCommentDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL comment DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddCommentDbContextFactory(this IServiceCollection services, PostgresCommentOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresCommentOptions>>(Options.Create(options));
        services.AddPostgresMigrations<CommentDbContext, PostgresCommentOptions>();
        services.AddDbContextFactory<CommentDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresCommentOptions.Schema)));

        return services;
    }

    /// <summary>Adds PostgreSQL comment DbContextFactory and PostgresCommentStore (ICommentStore) to the service collection.</summary>
    public static IServiceCollection AddPostgresCommentStore(this IServiceCollection services, Action<PostgresCommentOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresCommentOptions();
        configure(options);
        return services.AddPostgresCommentStore(options);
    }

    /// <summary>Adds PostgreSQL comment store using configuration binding.</summary>
    public static IServiceCollection AddPostgresCommentStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresCommentOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresCommentOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresCommentStore(options);
    }

    /// <summary>Adds PostgreSQL comment DbContextFactory and PostgresCommentStore to the service collection.</summary>
    public static IServiceCollection AddPostgresCommentStore(this IServiceCollection services, PostgresCommentOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddCommentDbContextFactory(options);
        services.AddSingleton<ICommentStore, PostgresCommentStore>();
        return services;
    }
}