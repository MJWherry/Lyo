using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lyo.Cache;
using Lyo.Diff;
using Lyo.Discord.Client;
using Lyo.Discord.Models;
using Lyo.Notification;

namespace Lyo.Discord.Bot.Commands.Settings;

/// <summary>Slash commands under <c>/settings</c> for guild-scoped bot configuration (Lyo API).</summary>
/// <remarks>
/// Discord (and DSharpPlus) do not allow a slash group to mix direct subcommands and nested subgroups. Everything hangs under subgroups: <c>/settings channels …</c>,
/// <c>/settings roles …</c>, and <c>/settings info …</c>.
/// </remarks>
[SlashCommandGroup(GuildSlashSettings.Settings.Name, GuildSlashSettings.Settings.Description)]
public sealed class GuildSettingsSlashCommands : ApplicationCommandModule
{
    /// <summary>Subcommands under <c>/settings channels</c>.</summary>
    [SlashCommandGroup(GuildSlashSettings.Channels.Name, GuildSlashSettings.Channels.Description)]
    public sealed class Channels : ApplicationCommandModule
    {
        public LyoDiscordClient LyoApi { get; set; } = null!;

        public ICacheService Cache { get; set; } = null!;

        public INotificationPublisher NotificationPublisher { get; set; } = null!;

        public IDiffService DiffService { get; set; } = null!;

        [SlashCommand(GuildSlashSettings.Channels.SetCommandChannel.Name, GuildSlashSettings.Channels.SetCommandChannel.Description)]
        public async Task SetCommandChannel(
            InteractionContext ctx,
            [Option(GuildSlashSettings.Channels.SetCommandChannel.Channel.Name, GuildSlashSettings.Channels.SetCommandChannel.Channel.Description)] DiscordChannel? channel = null)
        {
            DiscordCommandHelpers.RequireManageGuild(ctx);
            var target = channel ?? ctx.Channel;
            if (target.Type != ChannelType.Text && target.Type != ChannelType.News && target.Type != ChannelType.PublicThread && target.Type != ChannelType.PrivateThread)
                throw new DiscordCommandException("Pick a text-based channel (or run this in one).");

            try {
                var guildId = (long)ctx.Guild!.Id;
                var settings = await LyoApi.Guilds.GetSettingsAsync(guildId).ConfigureAwait(false) ?? throw new DiscordCommandException("Could not load guild settings.");
                settings.CommandChannelId = target.Id;
                settings.NormalizeForPersistence();
                var updated = await LyoApi.Guilds.PutSettingsAsync(guildId, settings).ConfigureAwait(false);
                Cache.SetGuildSettings(DiffService, NotificationPublisher, guildId, updated, "slash-command");
                await ctx.CreateResponseAsync($"Command channel set to {target.Mention}.", true).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not DiscordCommandException) {
                throw new DiscordCommandException("Could not save settings.", ex);
            }
        }

        [SlashCommand(GuildSlashSettings.Channels.SetLogChannel.Name, GuildSlashSettings.Channels.SetLogChannel.Description)]
        public async Task SetLogChannel(
            InteractionContext ctx,
            [Option(GuildSlashSettings.Channels.SetLogChannel.Channel.Name, GuildSlashSettings.Channels.SetLogChannel.Channel.Description)] DiscordChannel? channel = null)
        {
            DiscordCommandHelpers.RequireManageGuild(ctx);
            if (channel != null && channel.Type != ChannelType.Text && channel.Type != ChannelType.News && channel.Type != ChannelType.PublicThread &&
                channel.Type != ChannelType.PrivateThread)
                throw new DiscordCommandException("Pick a text-based channel.");

            try {
                var guildId = (long)ctx.Guild!.Id;
                var settings = await LyoApi.Guilds.GetSettingsAsync(guildId).ConfigureAwait(false) ?? throw new DiscordCommandException("Could not load guild settings.");
                settings.LogChannelId = channel?.Id;
                settings.NormalizeForPersistence();
                var updated = await LyoApi.Guilds.PutSettingsAsync(guildId, settings).ConfigureAwait(false);
                Cache.SetGuildSettings(DiffService, NotificationPublisher, guildId, updated, "slash-command");
                var msg = channel == null ? "Log channel cleared." : $"Log channel set to {channel.Mention}.";
                await ctx.CreateResponseAsync(msg, true).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not DiscordCommandException) {
                throw new DiscordCommandException("Could not save settings.", ex);
            }
        }
    }

    /// <summary>Subcommands under <c>/settings roles</c>.</summary>
    [SlashCommandGroup(GuildSlashSettings.Roles.Name, GuildSlashSettings.Roles.Description)]
    public sealed class Roles : ApplicationCommandModule
    {
        public LyoDiscordClient LyoApi { get; set; } = null!;

        public ICacheService Cache { get; set; } = null!;

        public INotificationPublisher NotificationPublisher { get; set; } = null!;

        public IDiffService DiffService { get; set; } = null!;

        [SlashCommand(GuildSlashSettings.Roles.SetModRole.Name, GuildSlashSettings.Roles.SetModRole.Description)]
        public async Task SetModRole(
            InteractionContext ctx,
            [Option(GuildSlashSettings.Roles.SetModRole.Role.Name, GuildSlashSettings.Roles.SetModRole.Role.Description)] DiscordRole? role = null)
            => await SetRoleAsync(ctx, role, s => s.ModRoleId = role?.Id, "Moderator").ConfigureAwait(false);

        [SlashCommand(GuildSlashSettings.Roles.SetAdminRole.Name, GuildSlashSettings.Roles.SetAdminRole.Description)]
        public async Task SetAdminRole(
            InteractionContext ctx,
            [Option(GuildSlashSettings.Roles.SetAdminRole.Role.Name, GuildSlashSettings.Roles.SetAdminRole.Role.Description)] DiscordRole? role = null)
            => await SetRoleAsync(ctx, role, s => s.AdminRoleId = role?.Id, "Admin").ConfigureAwait(false);

        private async Task SetRoleAsync(InteractionContext ctx, DiscordRole? role, Action<DiscordGuildSettings> apply, string label)
        {
            DiscordCommandHelpers.RequireManageGuild(ctx);
            try {
                var guildId = (long)ctx.Guild!.Id;
                var settings = await LyoApi.Guilds.GetSettingsAsync(guildId).ConfigureAwait(false) ?? throw new DiscordCommandException("Could not load guild settings.");
                apply(settings);
                settings.NormalizeForPersistence();
                var updated = await LyoApi.Guilds.PutSettingsAsync(guildId, settings).ConfigureAwait(false);
                Cache.SetGuildSettings(DiffService, NotificationPublisher, guildId, updated, "slash-command");
                var msg = role == null ? $"{label} role cleared." : $"{label} role set to {role.Mention}.";
                await ctx.CreateResponseAsync(msg, true).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not DiscordCommandException) {
                throw new DiscordCommandException("Could not save settings.", ex);
            }
        }
    }

    /// <summary>Subcommands under <c>/settings info</c>.</summary>
    [SlashCommandGroup(GuildSlashSettings.Info.Name, GuildSlashSettings.Info.Description)]
    public sealed class Info : ApplicationCommandModule
    {
        public LyoDiscordClient LyoApi { get; set; } = null!;

        [SlashCommand(GuildSlashSettings.Info.Show.Name, GuildSlashSettings.Info.Show.Description)]
        public async Task Show(InteractionContext ctx)
        {
            DiscordCommandHelpers.RequireManageGuild(ctx);
            var guildId = (long)ctx.Guild!.Id;
            var settings = await LyoApi.Guilds.GetSettingsAsync(guildId).ConfigureAwait(false) ?? throw new DiscordCommandException("Could not load guild settings.");
            var embed = GuildSettingsEmbedBuilder.Build(settings, ctx.Guild);
            await ctx.CreateResponseAsync(embed, true).ConfigureAwait(false);
        }
    }
}