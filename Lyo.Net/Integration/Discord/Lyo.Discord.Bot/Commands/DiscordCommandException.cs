namespace Lyo.Discord.Bot.Commands;

/// <summary>Raised when a slash command should fail with a user-visible message (handled by <see cref="SlashCommandErrorResponder" />).</summary>
public sealed class DiscordCommandException : Exception
{
    /// <summary>Builds an exception whose <see cref="Exception.Message" /> is shown to the user (ephemeral).</summary>
    public DiscordCommandException(string userMessage)
        : base(userMessage) { }

    /// <summary>Produces an exception with a user message and an inner exception (logged in full; only <paramref name="userMessage" /> is shown to the user).</summary>
    public DiscordCommandException(string userMessage, Exception innerException)
        : base(userMessage, innerException) { }
}