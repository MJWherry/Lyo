using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Postgres;

/// <summary>Extension methods for registering PostgreSQL migration hosted services.</summary>
public static class Extensions
{
    /// <summary>Adds PostgresMigrationHostedService to run migrations at host startup when EnableAutoMigrations is true.</summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPostgresMigrations<TContext, TOptions>(this IServiceCollection services)
        where TContext : DbContext where TOptions : class, IPostgresMigrationConfig
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddHostedService<PostgresMigrationHostedService<TContext, TOptions>>();
        return services;
    }
}