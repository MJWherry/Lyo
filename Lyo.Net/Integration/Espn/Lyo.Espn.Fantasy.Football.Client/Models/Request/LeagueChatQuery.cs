namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Query options for league message board topics.</summary>
public sealed record LeagueChatQuery
{
    /// <summary>topic groups to request from the league message board. May be omitted.</summary>
    public IReadOnlyList<string> TopicTypes { get; init; } = [];
}