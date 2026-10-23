using System.Text.Json;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>ESPN topic list. Bound by the normal serializer, then mapped to <see cref="Models.Response.ChatConversationRes" />.</summary>
internal sealed class EspnChatTopicList
{
    /// <summary>Topics in the list response.</summary>
    public List<EspnChatConversation>? Topics { get; set; }
}

/// <summary>ESPN chat topic. Property names match the camel-case payload.</summary>
internal sealed class EspnChatConversation
{
    /// <summary>Topic id.</summary>
    public string? Id { get; set; }

    /// <summary>Author SWID, or <c>LM</c>.</summary>
    public string? Author { get; set; }

    /// <summary>Unix milliseconds.</summary>
    public long? Date { get; set; }

    /// <summary>Unix milliseconds of the last edit.</summary>
    public long? DateEdited { get; set; }

    /// <summary>True when ESPN has deleted the topic.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>True when ESPN has edited the topic.</summary>
    public bool IsEdited { get; set; }

    /// <summary>Message page. The topic list omits this.</summary>
    public List<EspnChatMessage>? Messages { get; set; }

    /// <summary>Messages in the topic.</summary>
    public int TotalMessageCount { get; set; }

    /// <summary>ESPN topic string.</summary>
    public string? Type { get; set; }

    /// <summary>Unread count.</summary>
    public int UnreadMessageCount { get; set; }

    /// <summary>Topic subject.</summary>
    public string? Subject { get; set; }

    /// <summary>SWIDs that can see the topic.</summary>
    public List<string>? ViewableBy { get; set; }
}

/// <summary>ESPN chat message. <see cref="Attributes" /> is classified and then dropped.</summary>
internal sealed class EspnChatMessage
{
    /// <summary>Message id.</summary>
    public string? Id { get; set; }

    /// <summary>Author SWID, or <c>LM</c>.</summary>
    public string? Author { get; set; }

    /// <summary>Unix milliseconds.</summary>
    public long Date { get; set; }

    /// <summary>True when ESPN has deleted the message.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>True when ESPN has edited the message.</summary>
    public bool IsEdited { get; set; }

    /// <summary>Topic id.</summary>
    public string? TopicId { get; set; }

    /// <summary>ESPN message type id.</summary>
    public int MessageTypeId { get; set; }

    /// <summary>Text body.</summary>
    public string? Content { get; set; }

    /// <summary>Trade and add/drop fields such as <c>team1addP1</c> and <c>tId</c>.</summary>
    public Dictionary<string, JsonElement>? Attributes { get; set; }
}