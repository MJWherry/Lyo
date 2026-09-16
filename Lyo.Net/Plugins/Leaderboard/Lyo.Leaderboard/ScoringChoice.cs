namespace Lyo.Leaderboard;

/// <summary>Dropdown option. The key is the type a formula can branch on. <see cref="Value" /> is the number the formula adds.</summary>
public sealed class ScoringChoice
{
    /// <summary>Stable stored key (e.g. <c>bass</c>). Formulas read it as <c>{property}_key</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Human-readable label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Number substituted for the property key when this option is selected.</summary>
    public double Value { get; set; }
}
