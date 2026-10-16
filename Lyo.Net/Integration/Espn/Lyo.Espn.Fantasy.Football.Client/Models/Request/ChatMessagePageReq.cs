namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Body of the <c>x-fantasy-filter</c> header for <c>view=chat_conversation</c>.</summary>
internal sealed record ChatMessagePageReq
{
    public int Limit { get; init; }

    public int Offset { get; init; }

    public SortReq? SortDate { get; init; }
}
