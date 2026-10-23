using System.Diagnostics;

namespace Lyo.FeatureFlags.AspNetCore;

/// <summary>How <see cref="FeatureFlagMiddleware" /> builds a caller. Set this from <c>UseFeatureFlags</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FeatureFlagEndpointOptions
{
    /// <summary>Claim copied into <c>FeatureFlagContext.TenantId</c>. Lyo JWTs have no tenant claim, so this stays unset until a host names one.</summary>
    public string? TenantClaimType { get; set; }

    /// <summary>
    /// Replaces the default user, tenant, and claim mapping. A null return is an anonymous caller: on flags still pass, and rules and partial percentages do not. Attributes stay
    /// empty unless this factory sets them.
    /// </summary>
    public Func<HttpContext, FeatureFlagContext?>? ContextFactory { get; set; }

    /// <summary>Tenant claim type and whether a context factory is set.</summary>
    public override string ToString() => $"TenantClaimType={TenantClaimType} ContextFactorySet={ContextFactory is not null}";
}