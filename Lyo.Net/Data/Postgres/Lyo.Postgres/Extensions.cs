using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Postgres;

/// <summary>DI helpers that register PostgreSQL contexts and the startup migration worker.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="PostgresMigrationHostedService{TContext,TOptions}" /> so migrations run at host start when EnableAutoMigrations is on.</summary>
        /// <returns>The same collection so calls can be chained.</returns>
        public IServiceCollection AddPostgresMigrations<TContext, TOptions>()
            where TContext : DbContext where TOptions : class, IPostgresMigrationConfig
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddHostedService<PostgresMigrationHostedService<TContext, TOptions>>();
            return services;
        }

        /// <summary>Registers options, the startup migrator, and an <see cref="IDbContextFactory{TContext}" /> whose history table lives in the package schema.</summary>
        /// <remarks>
        /// A <c>*.Postgres</c> package can call this from its own <c>Add{Feature}DbContextFactory</c> and stop there. <typeparamref name="TOptions" /> supplies the
        /// schema, so migration history stays in that schema and several packages can share one database.
        /// <para><paramref name="options" /> is validated first. A bad connection string fails here instead of on the first query.</para>
        /// </remarks>
        /// <param name="options">Package options. Stored as <c>IOptions&lt;TOptions&gt;</c> and as <typeparamref name="TOptions" />.</param>
        /// <returns>The same collection so calls can be chained.</returns>
        /// <exception cref="ArgumentException">Raised when <paramref name="options" /> fails <see cref="PostgresOptionsBase.Validate" />.</exception>
        public IServiceCollection AddPostgresDbContextFactory<TContext, TOptions>(TOptions options)
            where TContext : DbContext where TOptions : PostgresOptionsBase
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();

            var schema = ((IPostgresMigrationConfig)options).Schema;
            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.AddPostgresMigrations<TContext, TOptions>();
            services.AddDbContextFactory<TContext>(dbOptions => dbOptions.UseNpgsql(
                options.ConnectionString, npgsql => npgsql.MigrationsHistoryTable(PostgresSchema.MigrationsHistoryTable, schema)));

            return services;
        }
    }
}
