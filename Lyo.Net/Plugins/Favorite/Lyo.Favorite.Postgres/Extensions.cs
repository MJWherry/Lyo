using Lyo.Configuration;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Favorite.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Favorite.Postgres;

/// <summary>DI helpers for PostgreSQL favorite store registration.</summary>
public static class Extensions
{
    /// <summary>Registers PostgreSQL favorite DbContextFactory to the service collection (IDbContextFactory only).</summary>
    public static IServiceCollection AddFavoriteDbContextFactory(this IServiceCollection services, Action<PostgresFavoriteOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new PostgresFavoriteOptions();
        configure(options);
        return services.AddFavoriteDbContextFactory(options);
    }

    /// <summary>Registers PostgreSQL favorite DbContextFactory using configuration binding.</summary>
    public static IServiceCollection AddFavoriteDbContextFactoryFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresFavoriteOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        var options = LyoOptions.Bind<PostgresFavoriteOptions>(configuration, configSectionName);

        return services.AddFavoriteDbContextFactory(options);
    }

    /// <summary>Registers PostgreSQL favorite DbContextFactory to the service collection.</summary>
    public static IServiceCollection AddFavoriteDbContextFactory(this IServiceCollection services, PostgresFavoriteOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        services.AddPostgresDbContextFactory<FavoriteDbContext, PostgresFavoriteOptions>(options);
        services.AddEntityRefOptions();

        return services;
    }

    /// <summary>Registers PostgreSQL favorite DbContextFactory and PostgresFavoriteStore (IFavoriteStore) to the service collection.</summary>
    public static IServiceCollection AddPostgresFavoriteStore(this IServiceCollection services, Action<PostgresFavoriteOptions> configure)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configure);
        var options = new PostgresFavoriteOptions();
        configure(options);
        return services.AddPostgresFavoriteStore(options);
    }

    /// <summary>Registers PostgreSQL favorite store using configuration binding.</summary>
    public static IServiceCollection AddPostgresFavoriteStoreFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = PostgresFavoriteOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
        var options = LyoOptions.Bind<PostgresFavoriteOptions>(configuration, configSectionName);

        return services.AddPostgresFavoriteStore(options);
    }

    /// <summary>Registers PostgreSQL favorite DbContextFactory and PostgresFavoriteStore to the service collection.</summary>
    public static IServiceCollection AddPostgresFavoriteStore(this IServiceCollection services, PostgresFavoriteOptions options)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(options);
        services.AddFavoriteDbContextFactory(options);
        services.AddSingleton<IFavoriteStore>(sp => new PostgresFavoriteStore(
            sp.GetRequiredService<IDbContextFactory<FavoriteDbContext>>(), sp.GetRequiredService<EntityRefOptions>(),
            sp.GetRequiredService<PostgresFavoriteOptions>(), sp.GetServices<IEntityRefActionInterceptor>()));

        return services;
    }
}