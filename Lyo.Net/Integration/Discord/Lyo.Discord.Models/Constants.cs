namespace Lyo.Discord.Models;

/// <summary>Consolidated constants for the Discord integration.</summary>
public static class Constants
{
    /// <summary>REST API route segments (group name "Discord").</summary>
    public static class Rest
    {
        public static class Discord
        {
            public const string Route = "Discord";

            public const string Users = $"{Route}/User";

            public const string Guilds = $"{Route}/Guild";

            public const string Channels = $"{Route}/Channel";

            public const string Emojis = $"{Route}/Emoji";

            public const string Roles = $"{Route}/Role";

            public const string Interactions = $"{Route}/Interaction";

            public const string Messages = $"{Route}/Message";

            public const string Attachments = $"{Route}/Attachment";

            public const string Members = $"{Route}/Member";

            /// <summary>GET/PUT guild-scoped config: <c>Discord/Guild/{guildId}/GuildSettings</c>.</summary>
            public static string GuildSettings(long guildId) => $"{Guilds}/{guildId}/GuildSettings";

            /// <summary>GET binding revision history: <c>Discord/Guild/{guildId}/GuildSettings/Revisions</c>.</summary>
            public static string GuildSettingsRevisions(long guildId) => $"{Guilds}/{guildId}/GuildSettings/Revisions";

            /// <summary>POST revert to a revision: <c>Discord/Guild/{guildId}/GuildSettings/Revert/{revision}</c>.</summary>
            public static string GuildSettingsRevert(long guildId, int revision) => $"{Guilds}/{guildId}/GuildSettings/Revert/{revision}";
        }
    }
}