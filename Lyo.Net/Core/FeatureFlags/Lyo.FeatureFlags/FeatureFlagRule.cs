namespace Lyo.FeatureFlags;

/// <summary>Predicates that must all match. Rules on one flag are OR; fields inside one rule are AND.</summary>
public sealed class FeatureFlagRule
{
    /// <summary>Caller user id, when this rule is limited to one user.</summary>
    public string? UserId { get; set; }

    /// <summary>Caller tenant id, when this rule is limited to one tenant.</summary>
    public string? TenantId { get; set; }

    /// <summary>Claims that must all be present. Any caller pair with the same type and value satisfies one entry. Null is treated as empty.</summary>
    public List<FeatureFlagClaim> Claims { get; set; } = [];

    /// <summary>Attributes that must all be present. Any caller pair with the same key and value satisfies one entry. Null is treated as empty.</summary>
    public List<FeatureFlagAttribute> Attributes { get; set; } = [];
}