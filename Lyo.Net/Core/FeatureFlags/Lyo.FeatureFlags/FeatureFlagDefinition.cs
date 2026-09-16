namespace Lyo.FeatureFlags;

/// <summary>One named flag. The dictionary key is the name callers pass to <see cref="IFeatureFlagService.IsEnabled" />.</summary>
public sealed class FeatureFlagDefinition
{
    /// <summary>Canonical name used for percentage hashing. <see cref="FeatureFlagOptions.Validate" /> sets this from the dictionary key.</summary>
    public string Name { get; set; } = "";

    /// <summary>Decision mode. Default <see cref="FeatureFlagMode.Off" />.</summary>
    public FeatureFlagMode Mode { get; set; }

    /// <summary>Rollout percent from 0 through 100 when <see cref="Mode" /> is <see cref="FeatureFlagMode.Percentage" />. Null is rejected for that mode.</summary>
    public int? Percentage { get; set; }

    /// <summary>Stickiness for a percentage flag. Ignored for other modes. Default <see cref="FeatureFlagStickiness.User" />.</summary>
    public FeatureFlagStickiness Stickiness { get; set; } = FeatureFlagStickiness.User;

    /// <summary>Attribute key hashed when <see cref="Stickiness" /> is <see cref="FeatureFlagStickiness.Attribute" />.</summary>
    public string? StickinessAttribute { get; set; }

    /// <summary>OR rules used when <see cref="Mode" /> is <see cref="FeatureFlagMode.Rules" />. Null is treated as empty.</summary>
    public List<FeatureFlagRule> Rules { get; set; } = [];
}
