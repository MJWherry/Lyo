namespace Lyo.Authorization.AspNetCore;

/// <summary>
/// Builds <c>grant|…</c> policies and forwards every other name, including the default and fallback policies.
/// </summary>
public sealed class GrantAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly IAuthorizationPolicyProvider _inner;

    /// <summary>Wraps the provider that was registered before <c>AddLyoGrantAuthorization</c>.</summary>
    public GrantAuthorizationPolicyProvider(IAuthorizationPolicyProvider inner)
    {
        Lyo.Exceptions.ArgumentHelpers.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _inner.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _inner.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!GrantPolicyNames.TryParse(policyName, out var action, out var resourceType, out var resourceRoute, out var tenantRoute))
            return _inner.GetPolicyAsync(policyName);

        var builder = new AuthorizationPolicyBuilder();
        builder.RequireAuthenticatedUser();
        builder.AddRequirements(new GrantAuthorizationRequirement(action, resourceType, resourceRoute, tenantRoute));
        return Task.FromResult<AuthorizationPolicy?>(builder.Build());
    }
}
