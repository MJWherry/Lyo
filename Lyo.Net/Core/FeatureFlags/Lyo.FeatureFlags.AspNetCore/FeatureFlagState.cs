namespace Lyo.FeatureFlags.AspNetCore;

/// <summary>One flag returned by <c>GET /features/manage</c>. Rules and percentages are not included.</summary>
public sealed class FeatureFlagState
{
    /// <summary>Flag name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Mode name: Off, On, Percentage, or Rules.</summary>
    public string Mode { get; set; } = "";
}

/// <summary>Body for <c>PUT /features/manage/{name}</c>. False clears that flag's percentage and rules.</summary>
public sealed class FeatureFlagEnabledRequest
{
    /// <summary>True sets the flag on. False sets it off.</summary>
    public bool Enabled { get; set; }
}