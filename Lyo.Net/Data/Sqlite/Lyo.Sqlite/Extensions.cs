using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Sqlite;

/// <summary>Extension methods for registering SQLite migration hosted services.</summary>
public static class Extensions
{
    /// <summary>Adds <see cref="SqliteMigrationHostedService{TContext,TOptions}" /> to run migrations at host startup when enabled.</summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TOptions">The options type implementing <see cref="ISqliteMigrationConfig" />.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqliteMigrations<TContext, TOptions>(this IServiceCollection services)
        where TContext : DbContext where TOptions : class, ISqliteMigrationConfig
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddHostedService<SqliteMigrationHostedService<TContext, TOptions>>();
        return services;
    }
}