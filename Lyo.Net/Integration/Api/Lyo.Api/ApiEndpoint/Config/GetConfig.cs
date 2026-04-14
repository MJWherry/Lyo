using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Get endpoint config. Use GetConfig&lt;object, DbContext&gt; for dynamic endpoints.</summary>
public sealed record GetConfig<TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<GetContext<TEntity, TDbContext>>? Before { get; init; }

    public Action<GetContext<TEntity, TDbContext>>? After { get; init; }

    public EndpointAuth? Auth { get; init; }
}