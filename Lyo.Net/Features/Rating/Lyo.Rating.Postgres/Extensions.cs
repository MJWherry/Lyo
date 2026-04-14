using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Rating.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Rating.Postgres;

/// <summary>Extension methods for PostgreSQL rating store registration.</summary>
public static class Extensions
{
    /// <summary>Adds PostgreSQL rating DbContextFactory to the service collection (IDbContextFactory only).</summary>
    public static IServiceCollection AddRatingDbContextFactory(this IServiceCollection services, Action<PostgresRatingOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresRatingOptions();
        configure(options);
        return services.AddRatingDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL rating DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddRatingDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresRatingOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresRatingOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddRatingDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL rating DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddRatingDbContextFactory(this IServiceCollection services, PostgresRatingOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton<IOptions<PostgresRatingOptions>>(Options.Create(options));
        services.AddPostgresMigrations<RatingDbContext, PostgresRatingOptions>();
        services.AddDbContextFactory<RatingDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresRatingOptions.Schema)));

        return services;
    }

    /// <summary>Adds PostgreSQL rating DbContextFactory and PostgresRatingStore (IRatingStore) to the service collection.</summary>
    public static IServiceCollection AddPostgresRatingStore(this IServiceCollection services, Action<PostgresRatingOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var options = new PostgresRatingOptions();
        configure(options);
        return services.AddPostgresRatingStore(options);
    }

    /// <summary>Adds PostgreSQL rating store using configuration binding.</summary>
    public static IServiceCollection AddPostgresRatingStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresRatingOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
        var options = new PostgresRatingOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresRatingStore(options);
    }

    /// <summary>Adds PostgreSQL rating DbContextFactory and PostgresRatingStore to the service collection.</summary>
    public static IServiceCollection AddPostgresRatingStore(this IServiceCollection services, PostgresRatingOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        services.AddRatingDbContextFactory(options);
        services.AddSingleton<IRatingStore, PostgresRatingStore>();
        return services;
    }
}