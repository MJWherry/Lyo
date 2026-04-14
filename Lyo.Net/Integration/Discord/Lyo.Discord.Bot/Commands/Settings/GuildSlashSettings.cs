namespace Lyo.Discord.Bot.Commands.Settings;

/// <summary>Centralized slash command and option names/descriptions for guild settings (<c>/settings</c> tree). Use with DSharpPlus attributes (compile-time string constants).</summary>
public static class GuildSlashSettings
{
    /// <summary>Top-level group: <c>/settings</c>.</summary>
    public static class Settings
    {
        public const string Name = "settings";
        public const string Description = "Configure Lyo bot settings for this server";
    }

    /// <summary>Subgroup: <c>/settings channels</c>.</summary>
    public static class Channels
    {
        public const string Name = "channels";
        public const string Description = "Where the bot accepts commands";

        /// <summary>Command: <c>/settings channels setcommandchannel</c>.</summary>
        public static class SetCommandChannel
        {
            public const string Name = "setcommandchannel";
            public const string Description = "Set the channel where bot commands are accepted";

            /// <summary>Option for the target channel.</summary>
            public static class Channel
            {
                public const string Name = "channel";
                public const string Description = "Channel (omit to use the channel where you run this command)";
            }
        }

        /// <summary>Command: <c>/settings channels setlogchannel</c>.</summary>
        public static class SetLogChannel
        {
            public const string Name = "setlogchannel";
            public const string Description = "Set the channel where the bot posts server errors and command notices";

            public static class Channel
            {
                public const string Name = "channel";
                public const string Description = "Channel (omit to clear the log channel)";
            }
        }
    }

    /// <summary>Subgroup: <c>/settings roles</c>.</summary>
    public static class Roles
    {
        public const string Name = "roles";
        public const string Description = "Moderator and admin roles for bot permissions";

        /// <summary>Command: <c>/settings roles setmodrole</c>.</summary>
        public static class SetModRole
        {
            public const string Name = "setmodrole";
            public const string Description = "Set the moderator role used by the bot";

            public static class Role
            {
                public const string Name = "role";
                public const string Description = "Role (omit to clear)";
            }
        }

        /// <summary>Command: <c>/settings roles setadminrole</c>.</summary>
        public static class SetAdminRole
        {
            public const string Name = "setadminrole";
            public const string Description = "Set the admin role used by the bot";

            public static class Role
            {
                public const string Name = "role";
                public const string Description = "Role (omit to clear)";
            }
        }
    }

    /// <summary>Subgroup: <c>/settings info</c>.</summary>
    public static class Info
    {
        public const string Name = "info";
        public const string Description = "View current bot settings";

        /// <summary>Command: <c>/settings info show</c>.</summary>
        public static class Show
        {
            public const string Name = "show";
            public const string Description = "Show this server's Lyo bot settings";
        }
    }
}