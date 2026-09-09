namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Query options for recent activity from league communication.</summary>
public sealed record RecentActivityQuery
{
    /// <summary>Upper bound on topics returned.</summary>
    public int Limit { get; init; } = 25;

    /// <summary>How many topics to skip.</summary>
    public int Offset { get; init; }

    /// <summary>Cap on messages ESPN considers in each set.</summary>
    public int LimitPerMessageSet { get; init; } = 25;

    /// <summary>ESPN message type ids to include.</summary>
    public IReadOnlyList<int> MessageTypeIds { get; init; } = [178, 180, 179, 239, 181, 244];
}