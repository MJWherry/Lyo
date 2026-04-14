namespace Lyo.Discord.Bot.Commands;

/// <summary>Thrown when a slash command should fail with a user-visible message (handled by <see cref="SlashCommandErrorResponder" />).</summary>
public sealed class DiscordCommandException : Exception
{
    /// <summary>Creates an exception whose <see cref="Exception.Message" /> is shown to the user (ephemeral).</summary>
    public DiscordCommandException(string userMessage)
        : base(userMessage) { }

    /// <summary>Creates an exception with a user message and an inner exception (logged in full; only <paramref name="userMessage" /> is shown to the user).</summary>
    public DiscordCommandException(string userMessage, Exception innerException)
        : base(userMessage, innerException) { }
}