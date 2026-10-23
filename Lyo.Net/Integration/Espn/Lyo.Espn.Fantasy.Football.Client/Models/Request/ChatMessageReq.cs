using System.Diagnostics;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Body posted when a member sends a Fantasy Chat message. <see cref="MessageTypeId" /> <c>0</c> is the user text the chat app shows.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ChatMessageReq(string Author, string Content, int MessageTypeId)
{
    /// <summary>Author and message type. Message text is omitted.</summary>
    public override string ToString() => $"{Author} Type={MessageTypeId} ContentLength={Content?.Length ?? 0}";
}