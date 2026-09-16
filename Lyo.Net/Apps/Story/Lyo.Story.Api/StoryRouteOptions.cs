using Microsoft.AspNetCore.Http;

namespace Lyo.Story.Api;

/// <summary>
/// How <c>MapStoryRoutes</c> binds callers. A host that serves players (such as a Game server) sets <see cref="PlayerRef" /> and <see cref="TenantId" /> from
/// claims; a trusted authoring host can leave them null to trust the request.
/// </summary>
public sealed class StoryRouteOptions
{
    /// <summary>
    /// Policy required to save or list graphs and to set playthrough variables. Null leaves those routes open to whoever passes the group's own authorization.
    /// </summary>
    public string? AuthorPolicy { get; set; }

    /// <summary>
    /// Resolves the caller's player handle. When set, starting a playthrough stores this value (the body <c>PlayerRef</c> is ignored, and a null result gets 401),
    /// and every playthrough read or move answers 403 unless the playthrough's <c>PlayerRef</c> matches.
    /// </summary>
    public Func<HttpContext, string?>? PlayerRef { get; set; }

    /// <summary>
    /// Resolves the caller's tenant. When set, graph reads and playthrough starts answer 404 for a story in another tenant, graph lists use this tenant instead of the
    /// body, and graph saves for another tenant get 403. A null result means the caller only sees stories without a tenant.
    /// </summary>
    public Func<HttpContext, Guid?>? TenantId { get; set; }
}
