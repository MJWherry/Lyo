namespace Lyo.Authorization.AspNetCore;

/// <summary>One grant check. Route names are read from <see cref="HttpRequest.RouteValues" />.</summary>
public sealed class GrantAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>Builds a requirement.</summary>
    public GrantAuthorizationRequirement(string action, string resourceType, string? resourceIdRoute, string? tenantRoute)
    {
        Action = action;
        ResourceType = resourceType;
        ResourceIdRoute = resourceIdRoute;
        TenantRoute = tenantRoute;
    }

    /// <summary>Action that must be granted.</summary>
    public string Action { get; }

    /// <summary>Resource type that must be granted.</summary>
    public string ResourceType { get; }

    /// <summary>Route value that supplies the resource id. Null means a wildcard check.</summary>
    public string? ResourceIdRoute { get; }

    /// <summary>Route value that supplies the tenant. A non-guid does not succeed and does not throw.</summary>
    public string? TenantRoute { get; }
}
