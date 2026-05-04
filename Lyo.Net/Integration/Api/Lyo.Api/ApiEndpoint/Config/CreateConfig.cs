using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Create endpoint config. Use CreateConfig&lt;object, object, DbContext&gt; for dynamic endpoints.</summary>
public sealed record CreateConfig<TRequest, TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<CreateContext<TRequest, TEntity, TDbContext>>? Before { get; init; }

    public Action<CreateContext<TRequest, TEntity, TDbContext>>? After { get; init; }

    /// <summary>When non-null, runs after EF save completes instead of the synchronous <see cref="After" /> delegate.</summary>
    public Func<CreateContext<TRequest, TEntity, TDbContext>, Task>? AfterAsync { get; init; }

    public EndpointAuth? Auth { get; init; }
}