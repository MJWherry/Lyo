using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Patch endpoint settings. Use PatchConfig&lt;object, DbContext&gt; for dynamic endpoints.</summary>
public sealed record PatchConfig<TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<PatchContext<TEntity, TDbContext>>? Before { get; init; }

    public Action<PatchContext<TEntity, TDbContext>>? After { get; init; }

    public EndpointAuth? Auth { get; init; }

    /// <summary>When set, restricts <see cref="PatchRequest.Properties" /> keys by policy or by custom logic.</summary>
    public PatchPropertyAuthorization? PropertyAuthorization { get; init; }
}