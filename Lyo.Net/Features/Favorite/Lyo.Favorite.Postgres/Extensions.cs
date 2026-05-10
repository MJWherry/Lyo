using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Favorite.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Favorite.Postgres;

/// <summary>Extension methods for PostgreSQL favorite store registration.</summary>
public static class Extensions
{
    /// <summary>Adds PostgreSQL favorite DbContextFactory to the service collection (IDbContextFactory only).</summary>
    public static IServiceCollection AddFavoriteDbContextFactory(this IServiceCollection services, Action<PostgresFavoriteOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new PostgresFavoriteOptions();
        configure(options);
        return services.AddFavoriteDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL favorite DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddFavoriteDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresFavoriteOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        var options = new PostgresFavoriteOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddFavoriteDbContextFactory(options);
    }

    /// <summary>Adds PostgreSQL favorite DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddFavoriteDbContextFactory(this IServiceCollection services, PostgresFavoriteOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
        services.AddSingleton(Options.Create(options));
        services.AddOptions<EntityRefOptions>();
        services.AddPostgresMigrations<FavoriteDbContext, PostgresFavoriteOptions>();
        services.AddDbContextFactory<FavoriteDbContext>(dbOpts => dbOpts.UseNpgsql(
            options.ConnectionString, npgsqlOpts => npgsqlOpts.MigrationsHistoryTable("__EFMigrationsHistory", PostgresFavoriteOptions.Schema)));

        return services;
    }

    /// <summary>Adds PostgreSQL favorite DbContextFactory and PostgresFavoriteStore (IFavoriteStore) to the service collection.</summary>
    public static IServiceCollection AddPostgresFavoriteStore(this IServiceCollection services, Action<PostgresFavoriteOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new PostgresFavoriteOptions();
        configure(options);
        return services.AddPostgresFavoriteStore(options);
    }

    /// <summary>Adds PostgreSQL favorite store using configuration binding.</summary>
    public static IServiceCollection AddPostgresFavoriteStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresFavoriteOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        var options = new PostgresFavoriteOptions();
        var section = configuration.GetSection(configSectionName);
        if (section.Exists())
            section.Bind(options);

        return services.AddPostgresFavoriteStore(options);
    }

    /// <summary>Adds PostgreSQL favorite DbContextFactory and PostgresFavoriteStore to the service collection.</summary>
    public static IServiceCollection AddPostgresFavoriteStore(this IServiceCollection services, PostgresFavoriteOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        services.AddFavoriteDbContextFactory(options);
        services.AddSingleton<IFavoriteStore>(
            sp => new PostgresFavoriteStore(
                sp.GetRequiredService<IDbContextFactory<FavoriteDbContext>>(),
                sp.GetRequiredService<IOptions<EntityRefOptions>>(),
                sp.GetServices<IEntityRefActionInterceptor>()));
        return services;
    }
}