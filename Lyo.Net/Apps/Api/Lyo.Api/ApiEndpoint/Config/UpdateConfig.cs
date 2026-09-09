using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint.Config;

/// <summary>Update endpoint settings. Use UpdateConfig&lt;object, object, DbContext&gt; for dynamic endpoints.</summary>
public sealed record UpdateConfig<TRequest, TEntity, TDbContext>
    where TDbContext : DbContext where TEntity : class
{
    public Action<UpdateContext<TRequest, TEntity, TDbContext>>? Before { get; init; }

    public Action<UpdateContext<TRequest, TEntity, TDbContext>>? After { get; init; }

    public EndpointAuth? Auth { get; init; }

    /// <summary>
    /// Per-property write rules, evaluated against the properties whose incoming value differs from the persisted entity. Share the same instance with the patch config so the
    /// restriction cannot be sidestepped by switching verb.
    /// </summary>
    public PatchPropertyAuthorization? PropertyAuthorization { get; init; }
}