using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Postgres;

/// <summary>Reachability checks for Lyo stores that sit on PostgreSQL.</summary>
/// <remarks>
/// A store's <c>CheckHealthAsync</c> asks whether the database answers, times the call, and tags the result with the schema it owns. That shape is the same in every
/// package, so implementations call here and only pass their schema.
/// </remarks>
public static class PostgresHealth
{
    /// <summary>Metadata key that names the schema a health result is about.</summary>
    public const string SchemaMetadataKey = "database";

    /// <summary>Probes connectivity with a context from <paramref name="contextFactory" />.</summary>
    /// <param name="contextFactory">Factory for the store context. One context is created and disposed per check.</param>
    /// <param name="schema">Schema written into health metadata.</param>
    /// <param name="ct">Token that cancels the probe.</param>
    /// <returns>Healthy when the database answers; otherwise unhealthy with the reason.</returns>
    public static async Task<HealthResult> CheckAsync<TContext>(IDbContextFactory<TContext> contextFactory, string schema, CancellationToken ct = default)
        where TContext : DbContext
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        var sw = Stopwatch.StartNew();
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            return await CheckAsync(context, schema, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <summary>Probes connectivity with an already-created context.</summary>
    /// <remarks>For stores that take a scoped <see cref="DbContext" /> instead of a factory. The context is left open.</remarks>
    /// <param name="context">Store context to probe.</param>
    /// <param name="schema">Schema written into health metadata.</param>
    /// <param name="ct">Token that cancels the probe.</param>
    /// <returns>Healthy when the database answers; otherwise unhealthy with the reason.</returns>
    public static async Task<HealthResult> CheckAsync(DbContext context, string schema, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(context);
        var sw = Stopwatch.StartNew();
        try
        {
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { [SchemaMetadataKey] = schema })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }
}
