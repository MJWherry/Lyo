namespace Lyo.FeatureFlags;

/// <summary>One claim the caller presents, or one claim a rule requires.</summary>
public sealed class FeatureFlagClaim
{
    /// <summary>Claim type, compared ordinal.</summary>
    public string? Type { get; set; }

    /// <summary>Claim value, compared ordinal. Scope values are exact.</summary>
    public string? Value { get; set; }
}

/// <summary>One caller attribute, or one attribute a rule requires.</summary>
public sealed class FeatureFlagAttribute
{
    /// <summary>Attribute key, compared ordinal.</summary>
    public string? Key { get; set; }

    /// <summary>Attribute value, compared ordinal.</summary>
    public string? Value { get; set; }
}