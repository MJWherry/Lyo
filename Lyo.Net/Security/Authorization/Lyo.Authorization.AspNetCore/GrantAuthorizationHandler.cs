using Lyo.Exceptions;

namespace Lyo.Authorization.AspNetCore;

/// <summary>
/// Succeeds when the caller holds the grant. It does not fail the requirement.
/// Anonymous callers are rejected by <c>RequireAuthenticatedUser</c> on the policy.
/// </summary>
public sealed class GrantAuthorizationHandler : AuthorizationHandler<GrantAuthorizationRequirement>
{
    private readonly IResourceAuthorization _authorization;

    /// <summary>Builds a handler. Do not pass a <c>DbContext</c>. The Postgres store opens one per call.</summary>
    public GrantAuthorizationHandler(IResourceAuthorization authorization)
    {
        ArgumentHelpers.ThrowIfNull(authorization);
        _authorization = authorization;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GrantAuthorizationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (context.Resource is not HttpContext http)
            return;

        var subject = Subject(context.User);
        if (subject is null)
            return;

        if (!TryRoute(http, requirement.ResourceIdRoute, out var resourceId))
            return;
        if (resourceId is not null && Guid.TryParse(resourceId, out var resourceGuid))
            resourceId = resourceGuid.ToString("D");

        if (!TryTenant(http, requirement.TenantRoute, out var tenant))
            return;

        var allowed = await _authorization.IsAllowedAsync(new GrantCheck {
            Subject = subject,
            Action = requirement.Action,
            ResourceType = requirement.ResourceType,
            ResourceId = resourceId,
            TenantId = tenant
        }, http.RequestAborted).ConfigureAwait(false);
        if (allowed)
            context.Succeed(requirement);
    }

    private static string? Subject(System.Security.Claims.ClaimsPrincipal user)
    {
        var lyoUser = user.FindFirst("lyo:user")?.Value;
        if (!string.IsNullOrWhiteSpace(lyoUser))
            return lyoUser;

        var sub = user.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(sub) ? null : sub;
    }

    private static bool TryRoute(HttpContext http, string? route, out string? value)
    {
        value = null;
        if (route is null)
            return true;
        if (!http.Request.RouteValues.TryGetValue(route, out var raw) || raw is null)
            return false;

        value = raw.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryTenant(HttpContext http, string? route, out Guid? tenant)
    {
        tenant = null;
        if (route is null)
            return true;
        if (!TryRoute(http, route, out var text) || text is null)
            return false;
        if (!Guid.TryParse(text, out var parsed))
            return false;

        tenant = parsed;
        return true;
    }
}
