using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client.Builders;

/// <summary>
/// Builds the JSON body for a Fantasy Chat text message. <c>messageTypeId</c> is <c>0</c>, the user text the chat app shows. <see cref="For" /> picks a
/// <see cref="ChatTopics" /> audience. <see cref="To(string)" /> picks another league member for a direct message.
/// </summary>
public sealed class ChatMessageBuilder
{
    private string? _content;

    /// <summary>Conversation UUID or topic type. Null when the message was started with <see cref="To(string)" />.</summary>
    public string? TopicId { get; }

    /// <summary>League member selected with <see cref="To(MemberRes)" />.</summary>
    public MemberRes? Recipient { get; internal set; }

    /// <summary>Display name or SWID selected with <see cref="To(string)" />.</summary>
    public string? RecipientName { get; internal set; }

    private ChatMessageBuilder() { }

    /// <summary>Starts a message for one topic. <paramref name="topicId" /> is the conversation UUID, or a <see cref="ChatTopics" /> value.</summary>
    public ChatMessageBuilder(string topicId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(topicId);
        TopicId = topicId;
    }

    /// <summary>Starts a message for a chat audience such as <see cref="ChatTopics.AllMembers" />.</summary>
    public static ChatMessageBuilder For(string topic) => new(topic);

    /// <summary>Starts a direct message to <paramref name="member" />.</summary>
    public static ChatMessageBuilder To(MemberRes member)
    {
        ArgumentHelpers.ThrowIfNull(member);
        return new() { Recipient = member };
    }

    /// <summary>Starts a direct message to a member SWID or a unique display name.</summary>
    public static ChatMessageBuilder To(string nameOrSwid)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(nameOrSwid);
        return new() { RecipientName = nameOrSwid };
    }

    /// <summary>Sets the text the other members will see.</summary>
    public ChatMessageBuilder Content(string content)
    {
        _content = content;
        return this;
    }

    /// <summary>Returns the post body. Blank content throws. <paramref name="swid" /> is stored as <c>author</c>.</summary>
    public ChatMessageReq Build(string swid)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(swid);
        var content = _content;
        OperationHelpers.ThrowIf(string.IsNullOrWhiteSpace(content), "Chat message content is required.");
        return new(swid, content!, 0);
    }
}

/// <summary>Chains <see cref="ChatMessageBuilder.For" /> into a direct-message recipient.</summary>
public static class ChatMessageBuilderRecipients
{
    /// <summary>Addresses <paramref name="message" /> to <paramref name="member" />. A non-direct audience throws when the message is posted.</summary>
    public static ChatMessageBuilder To(this ChatMessageBuilder message, MemberRes member)
    {
        ArgumentHelpers.ThrowIfNull(message);
        ArgumentHelpers.ThrowIfNull(member);
        message.Recipient = member;
        message.RecipientName = null;
        return message;
    }

    /// <summary>Addresses <paramref name="message" /> to a member SWID or a unique display name.</summary>
    public static ChatMessageBuilder To(this ChatMessageBuilder message, string nameOrSwid)
    {
        ArgumentHelpers.ThrowIfNull(message);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(nameOrSwid);
        message.Recipient = null;
        message.RecipientName = nameOrSwid;
        return message;
    }
}