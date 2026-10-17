namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>Fantasy Chat topic types. Pass these to <see cref="Builders.ChatMessageBuilder.For" /> instead of the raw ESPN strings.</summary>
public static class ChatTopics
{
    /// <summary>The league-wide chat every member can see.</summary>
    public const string AllMembers = "CHAT_ALL_MEMBERS";

    /// <summary>A conversation between two members. Pair it with <see cref="Builders.ChatMessageBuilder.To(string)" />.</summary>
    public const string DirectMessage = "CHAT_DIRECT_MESSAGE";
}
