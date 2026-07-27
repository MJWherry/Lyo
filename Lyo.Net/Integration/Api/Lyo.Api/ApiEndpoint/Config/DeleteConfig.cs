using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Delete endpoint config. Use DeleteConfig&lt;object, DbContext&gt; for dynamic endpoints.</summary>
public sealed record DeleteConfig<TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<DeleteContext<TEntity, TDbContext>>? Before { get; init; }

    /// <summary>Async pre-delete hook; runs after the synchronous <see cref="Before" /> when both are set.</summary>
    public Func<DeleteContext<TEntity, TDbContext>, CancellationToken, Task>? BeforeAsync { get; init; }

    public Action<DeleteContext<TEntity, TDbContext>>? After { get; init; }

    public string[]? Includes { get; init; }

    public EndpointAuth? Auth { get; init; }
}