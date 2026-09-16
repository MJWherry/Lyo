namespace Lyo.FeatureFlags;

/// <summary>Which caller value a percentage flag hashes. A missing configuration value stays <see cref="User" />.</summary>
public enum FeatureFlagStickiness
{
    /// <summary><see cref="FeatureFlagContext.UserId" /> only. Tenant id is not a fallback.</summary>
    User = 0,

    /// <summary><see cref="FeatureFlagContext.TenantId" /> only.</summary>
    Tenant = 1,

    /// <summary>The attribute named by <see cref="FeatureFlagDefinition.StickinessAttribute" />.</summary>
    Attribute = 2
}
