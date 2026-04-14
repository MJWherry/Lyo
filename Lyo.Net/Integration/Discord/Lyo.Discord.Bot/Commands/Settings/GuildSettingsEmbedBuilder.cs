using DSharpPlus.Entities;
using Lyo.Discord.Models;

namespace Lyo.Discord.Bot.Commands.Settings;

/// <summary>Builds <see cref="DiscordEmbed" />s for <see cref="DiscordGuildSettings" /> (Lyo bot configuration).</summary>
public static class GuildSettingsEmbedBuilder
{
    /// <summary>Discord blurple; matches common bot embed styling.</summary>
    public static readonly DiscordColor DefaultAccent = new(0x5865F2);

    /// <summary>Creates an embed summarizing guild settings, resolving IDs to mentions when the entity exists in <paramref name="guild" />.</summary>
    public static DiscordEmbed Build(DiscordGuildSettings settings, DiscordGuild guild)
    {
        settings.NormalizeForRead();
        return new DiscordEmbedBuilder().WithTitle($"{guild.Name} Configuration")
            .WithColor(DefaultAccent)
            .AddField("Command channel", FormatCommandChannel(settings.CommandChannelId, guild), true)
            .AddField("Log channel", FormatLogChannel(settings.LogChannelId, guild), true)
            .AddField("Admin role", FormatRole(settings.AdminRoleId, guild), true)
            .AddField("Moderator role", FormatRole(settings.ModRoleId, guild), true)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .WithFooter(FormatFooter(settings))
            .Build();
    }

    private static string FormatFooter(DiscordGuildSettings settings) => settings.Revision is { } r ? $"V{settings.Version} R{r}" : $"V{settings.Version}";

    private static string FormatCommandChannel(ulong? channelId, DiscordGuild guild)
    {
        if (channelId is null)
            return "Any channel";

        var ch = guild.GetChannel(channelId.Value);
        return ch != null ? ch.Mention : $"Unknown channel (`{channelId}`)";
    }

    private static string FormatLogChannel(ulong? channelId, DiscordGuild guild)
    {
        if (channelId is null)
            return "Not set";

        var ch = guild.GetChannel(channelId.Value);
        return ch != null ? ch.Mention : $"Unknown channel (`{channelId}`)";
    }

    private static string FormatRole(ulong? roleId, DiscordGuild guild)
    {
        if (roleId is null)
            return "Not set";

        var role = guild.GetRole(roleId.Value);
        return role != null ? role.Mention : $"Unknown role (`{roleId}`)";
    }
}