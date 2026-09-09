using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Sqlite;

/// <summary>DI helpers that register SQLite migration hosted services.</summary>
public static class Extensions
{
    /// <summary>Registers <see cref="SqliteMigrationHostedService{TContext,TOptions}" /> so migrations run at host start when the option is on.</summary>
    /// <typeparam name="TContext">DbContext to migrate.</typeparam>
    /// <typeparam name="TOptions">Options type that implements <see cref="ISqliteMigrationConfig" />.</typeparam>
    /// <param name="services">Collection to add the hosted service to.</param>
    /// <returns>The same collection so calls can be chained.</returns>
    public static IServiceCollection AddSqliteMigrations<TContext, TOptions>(this IServiceCollection services)
        where TContext : DbContext where TOptions : class, ISqliteMigrationConfig
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddHostedService<SqliteMigrationHostedService<TContext, TOptions>>();
        return services;
    }
}