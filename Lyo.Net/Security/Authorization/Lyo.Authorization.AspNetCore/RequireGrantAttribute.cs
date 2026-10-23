namespace Lyo.Authorization.AspNetCore;

/// <summary>Shorthand for a <c>grant|…</c> policy. Allowed more than once. A class-level attribute applies to every action.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireGrantAttribute : AuthorizeAttribute
{
    /// <summary>Requires <paramref name="action" /> on <paramref name="resourceType" />.</summary>
    public RequireGrantAttribute(string action, string resourceType, string? resourceIdRoute = null, string? tenantRoute = null)
        => Policy = GrantPolicyNames.For(action, resourceType, resourceIdRoute, tenantRoute);
}