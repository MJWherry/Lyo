namespace Lyo.FeatureFlags;

/// <summary>How a named flag decides. A missing configuration value stays <see cref="Off" />.</summary>
public enum FeatureFlagMode
{
    /// <summary>Always disabled.</summary>
    Off = 0,

    /// <summary>Always enabled. The caller context is ignored.</summary>
    On = 1,

    /// <summary>Enabled for a stable percentage of stickiness keys.</summary>
    Percentage = 2,

    /// <summary>Enabled when any rule matches the caller.</summary>
    Rules = 3
}