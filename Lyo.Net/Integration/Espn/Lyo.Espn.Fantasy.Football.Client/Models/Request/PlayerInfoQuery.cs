using System.Diagnostics;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Query options for player card details.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PlayerInfoQuery
{
    /// <summary>ESPN player ids to fetch.</summary>
    public IReadOnlyList<int> PlayerIds { get; init; } = [];

    /// <summary>The top scoring period to include on the player card stats filter.</summary>
    public int ScoringPeriodId { get; init; } = 18;

    /// <summary>Extra stat-period ids ESPN expects alongside the current season.</summary>
    public IReadOnlyList<string> AdditionalPeriodIds { get; init; } = [];
}