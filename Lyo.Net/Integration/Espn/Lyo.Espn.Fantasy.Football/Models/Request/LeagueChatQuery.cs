namespace Lyo.Espn.Fantasy.Football.Models.Request;

/// <summary>Options for loading league message board topics.</summary>
public sealed record LeagueChatQuery
{
    /// <summary>Optional topic groups to request from the league message board.</summary>
    public IReadOnlyList<string> TopicTypes { get; init; } = [];
}