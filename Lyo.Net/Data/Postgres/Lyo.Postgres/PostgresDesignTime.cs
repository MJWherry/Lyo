using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Postgres;

/// <summary>Helpers used by <c>IDesignTimeDbContextFactory</c> types that <c>dotnet ef</c> loads.</summary>
/// <remarks>
/// Design-time factories run under the EF CLI with no host, configuration, or service provider, so the connection string has to come from the environment. Every Lyo
/// package does the same three steps: read an environment variable, fail with a useful message when it is missing, then build schema-scoped options. Those steps live
/// here so each package factory only names its variable and schema.
/// </remarks>
/// <example>
/// <code>
/// public class TagDbContextFactory : IDesignTimeDbContextFactory&lt;TagDbContext&gt;
/// {
///     public TagDbContext CreateDbContext(string[] args)
///         => new(PostgresDesignTime.CreateOptions&lt;TagDbContext&gt;("TAG_CONNECTION_STRING", PostgresTagOptions.Schema));
/// }
/// </code>
/// </example>
public static class PostgresDesignTime
{
    /// <summary>Builds design-time context options from a connection string stored in an environment variable.</summary>
    /// <param name="environmentVariableName">Variable that holds the connection string, for example <c>TAG_CONNECTION_STRING</c>.</param>
    /// <param name="schema">Schema that owns the context tables and migration history.</param>
    /// <param name="fallbackConnectionString">Used when the variable is unset. Pass null to require the variable.</param>
    /// <returns>Options to hand to the context constructor.</returns>
    /// <exception cref="InvalidOperationException">Raised when the variable is unset and no fallback was given.</exception>
    public static DbContextOptions<TContext> CreateOptions<TContext>(string environmentVariableName, string schema, string? fallbackConnectionString = null)
        where TContext : DbContext
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(environmentVariableName);
        return CreateOptions<TContext>([environmentVariableName], schema, fallbackConnectionString);
    }

    /// <summary>Builds design-time context options, taking the connection string from the first environment variable that is set.</summary>
    /// <remarks>Accepts several names so a package can keep an older variable working next to the current one.</remarks>
    /// <param name="environmentVariableNames">Variables to try, first match wins.</param>
    /// <param name="schema">Schema that owns the context tables and migration history.</param>
    /// <param name="fallbackConnectionString">Used when none of the variables is set. Pass null to require one of them.</param>
    /// <returns>Options to hand to the context constructor.</returns>
    /// <exception cref="InvalidOperationException">Raised when no variable is set and no fallback was given.</exception>
    public static DbContextOptions<TContext> CreateOptions<TContext>(
        IReadOnlyList<string> environmentVariableNames, string schema, string? fallbackConnectionString = null)
        where TContext : DbContext
    {
        ArgumentHelpers.ThrowIfNull(environmentVariableNames);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(schema);
        if (environmentVariableNames.Count == 0)
            throw new ArgumentException("At least one environment variable name is required.", nameof(environmentVariableNames));

        var connectionString = environmentVariableNames
            .Select(Environment.GetEnvironmentVariable)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? fallbackConnectionString;

        OperationHelpers.ThrowIfNullOrWhiteSpace(
            connectionString,
            $"Set {string.Join(" or ", environmentVariableNames)} to a PostgreSQL connection string for design-time operations.");

        return PostgresSchema.BuildOptions<TContext>(connectionString, schema);
    }
}
