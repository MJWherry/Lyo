namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Body posted when a member sends a Fantasy Chat message. <see cref="MessageTypeId" /> <c>0</c> is the user text the chat app shows.</summary>
public sealed record ChatMessageReq(string Author, string Content, int MessageTypeId);