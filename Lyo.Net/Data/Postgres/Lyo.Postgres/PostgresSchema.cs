using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Postgres;

/// <summary>Shared PostgreSQL helpers that keep each Lyo <c>*.Postgres</c> package inside its own schema.</summary>
/// <remarks>
/// Every Lyo package owns one schema and stores migration history there. The two operations every package needs are building
/// <see cref="DbContextOptions{TContext}" /> aimed at that schema, and creating the schema before the first migration. Both sit here so the convention is written once.
/// </remarks>
public static class PostgresSchema
{
    /// <summary>EF Core migration-history table name used inside each package schema.</summary>
    /// <remarks>
    /// Packages keep EF Core's default table name but put it in their own schema, so they can share a database without sharing migration state.
    /// </remarks>
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    /// <summary>Builds <see cref="DbContextOptions{TContext}" /> for a connection string and pins the history table to <paramref name="schema" />.</summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="schema">Schema that owns the context tables and migration history.</param>
    /// <returns>Options a <typeparamref name="TContext" /> constructor can take.</returns>
    public static DbContextOptions<TContext> BuildOptions<TContext>(string connectionString, string schema)
        where TContext : DbContext
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(schema);
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable, schema))
            .Options;
    }

    /// <summary>Creates a <typeparamref name="TContext" /> without going through DI.</summary>
    /// <remarks>
    /// Use this on migration and tooling paths that have a connection string but no service provider. <typeparamref name="TContext" /> must take
    /// <see cref="DbContextOptions{TContext}" /> in its constructor, which every Lyo context does.
    /// </remarks>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="schema">Schema that owns the context tables and migration history.</param>
    /// <returns>A new context. The caller owns it and must dispose it.</returns>
    public static TContext CreateContext<TContext>(string connectionString, string schema)
        where TContext : DbContext
        => (TContext)Activator.CreateInstance(typeof(TContext), BuildOptions<TContext>(connectionString, schema))!;

    /// <summary>Creates <paramref name="schema" /> when it is missing.</summary>
    /// <remarks>
    /// <c>MigrateAsync</c> will not create the schema that holds the migration-history table, so this must run first on an empty database.
    /// </remarks>
    /// <param name="context">Context already pointed at the target database.</param>
    /// <param name="schema">Schema to create.</param>
    /// <param name="ct">Token that cancels the DDL.</param>
    public static Task EnsureAsync(DbContext context, string schema, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(context);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(schema);

        // DDL cannot take a parameterized schema name. Doubling embedded quotes keeps the quoted identifier valid.
        // The value comes from a package constant or host config, never from user input.
        var quoted = schema.Replace("\"", "\"\"");
#pragma warning disable EF1002 // Interpolation is a quoted DDL identifier, not a parameterizable value.
        return context.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{quoted}\";", ct);
#pragma warning restore EF1002
    }
}
