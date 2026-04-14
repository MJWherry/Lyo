namespace Lyo.Espn.Fantasy.Football.Models.Request;

/// <summary>Options for loading recent activity from league communication.</summary>
public sealed record RecentActivityQuery
{
    /// <summary>Maximum number of topics to return.</summary>
    public int Limit { get; init; } = 25;

    /// <summary>Number of topics to skip.</summary>
    public int Offset { get; init; }

    /// <summary>Maximum number of messages ESPN should consider per message set.</summary>
    public int LimitPerMessageSet { get; init; } = 25;

    /// <summary>Optional ESPN message type ids to include.</summary>
    public IReadOnlyList<int> MessageTypeIds { get; init; } = [178, 180, 179, 239, 181, 244];
}