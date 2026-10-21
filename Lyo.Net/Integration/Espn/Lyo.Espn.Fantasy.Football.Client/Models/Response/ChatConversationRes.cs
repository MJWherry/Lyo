using System.Diagnostics;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Response;

/// <summary>
/// One Fantasy Chat topic. Ids and unix times stay on the suffixed properties. <see cref="Author" />, <see cref="Date" />, <see cref="DateEdited" />, and
/// <see cref="Members" /> are the same values in a form you can use. The chat methods fill those from the league index when it already has them.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ChatConversationRes
{
    /// <summary>ESPN topic id.</summary>
    public string Id { get; }

    /// <summary>Author SWID, or <c>LM</c> for the league manager.</summary>
    public string? AuthorId { get; }

    /// <summary>League member for <see cref="AuthorId" />. Null for <c>LM</c>, a blank id, or a member the league index did not have.</summary>
    public MemberRes? Author { get; internal set; }

    /// <summary>True when <see cref="AuthorId" /> is the league-manager marker <c>LM</c>.</summary>
    public bool FromLeagueManager => LeagueIndex.IsLeagueManager(AuthorId);

    /// <summary>ESPN <c>date</c> as unix milliseconds.</summary>
    public long? DateUnix { get; }

    /// <summary><see cref="DateUnix" /> as a timestamp. Null when ESPN omitted the date.</summary>
    public DateTimeOffset? Date => EspnUnix.ToOffset(DateUnix);

    /// <summary>ESPN <c>dateEdited</c> as unix milliseconds.</summary>
    public long? DateEditedUnix { get; }

    /// <summary><see cref="DateEditedUnix" /> as a timestamp. Null when ESPN omitted it.</summary>
    public DateTimeOffset? DateEdited => EspnUnix.ToOffset(DateEditedUnix);

    /// <summary>True when ESPN has deleted the topic.</summary>
    public bool IsDeleted { get; }

    /// <summary>True when ESPN has edited the topic.</summary>
    public bool IsEdited { get; }

    /// <summary>Message page. The topic list omits this.</summary>
    public IReadOnlyList<ChatMessageRes>? Messages { get; }

    /// <summary>Messages in the topic, including ones outside this page.</summary>
    public int TotalMessageCount { get; }

    /// <summary>ESPN topic string, such as <c>CHAT_ALL_MEMBERS</c> or <c>CHAT_DIRECT_MESSAGE</c>.</summary>
    public string? Type { get; }

    /// <summary>Unread messages reported by ESPN.</summary>
    public int UnreadMessageCount { get; }

    /// <summary>Topic subject, when ESPN sends one.</summary>
    public string? Subject { get; }

    /// <summary>SWIDs that can see this topic. Direct messages list both people.</summary>
    public IReadOnlyList<string> ViewableByIds { get; }

    /// <summary><see cref="ViewableByIds" /> resolved to league members. Ids the index does not know stay on <see cref="ViewableByIds" /> only.</summary>
    public IReadOnlyList<MemberRes> Members { get; internal set; } = [];

    /// <summary>Builds a conversation from values already taken off the ESPN payload.</summary>
    public ChatConversationRes(
        string Id,
        string? AuthorId,
        long? DateUnix,
        long? DateEditedUnix,
        bool IsDeleted,
        bool IsEdited,
        IReadOnlyList<ChatMessageRes>? Messages,
        int TotalMessageCount,
        string? Type,
        int UnreadMessageCount,
        string? Subject = null,
        IReadOnlyList<string>? ViewableByIds = null)
    {
        this.Id = Id;
        this.AuthorId = AuthorId;
        this.DateUnix = DateUnix;
        this.DateEditedUnix = DateEditedUnix;
        this.IsDeleted = IsDeleted;
        this.IsEdited = IsEdited;
        this.Messages = Messages;
        this.TotalMessageCount = TotalMessageCount;
        this.Type = Type;
        this.UnreadMessageCount = UnreadMessageCount;
        this.Subject = Subject;
        this.ViewableByIds = ViewableByIds ?? [];
    }

    /// <summary>Topic id, type, and message count, for logs and the debugger.</summary>
    public override string ToString() => $"Conversation Id={Id} Type={Type} Messages={Messages?.Count ?? 0} Total={TotalMessageCount}";
}