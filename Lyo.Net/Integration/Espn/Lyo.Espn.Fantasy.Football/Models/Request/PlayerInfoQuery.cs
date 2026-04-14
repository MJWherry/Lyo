namespace Lyo.Espn.Fantasy.Football.Models.Request;

/// <summary>Options for loading player card details.</summary>
public sealed record PlayerInfoQuery
{
    /// <summary>The ESPN player ids to request.</summary>
    public IReadOnlyList<int> PlayerIds { get; init; } = [];

    /// <summary>The top scoring period to include in the player card stats filter.</summary>
    public int ScoringPeriodId { get; init; } = 18;

    /// <summary>Additional stat period identifiers ESPN expects alongside the current season.</summary>
    public IReadOnlyList<string> AdditionalPeriodIds { get; init; } = [];
}