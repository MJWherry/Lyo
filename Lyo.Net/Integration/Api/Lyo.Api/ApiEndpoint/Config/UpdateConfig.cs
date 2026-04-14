using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Update endpoint config. Use UpdateConfig&lt;object, object, DbContext&gt; for dynamic endpoints.</summary>
public sealed record UpdateConfig<TRequest, TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<UpdateContext<TRequest, TEntity, TDbContext>>? Before { get; init; }

    public Action<UpdateContext<TRequest, TEntity, TDbContext>>? After { get; init; }

    public EndpointAuth? Auth { get; init; }
}