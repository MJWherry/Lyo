namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Page window for one Fantasy Chat conversation. The chat app's first page is the newest 25 messages.</summary>
public sealed record ChatConversationQuery
{
    /// <summary>How many messages to return.</summary>
    public int Limit { get; init; } = 25;

    /// <summary>How many messages to skip.</summary>
    public int Offset { get; init; }

    /// <summary>True to sort oldest first. The first page is newest first.</summary>
    public bool SortAscending { get; init; }
}
