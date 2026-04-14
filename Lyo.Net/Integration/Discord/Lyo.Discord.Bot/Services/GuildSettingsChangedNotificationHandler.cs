using System.Globalization;
using System.Text;
using Lyo.Diff;
using Lyo.Discord.Models;
using Lyo.Notification;
using Microsoft.Extensions.Logging;

namespace Lyo.Discord.Bot.Services;

/// <summary>Logs guild settings changes to <see cref="ILogger" /> and to the guild log channel when a client is connected.</summary>
public sealed class GuildSettingsChangedNotificationHandler(
    ILogger<GuildSettingsChangedNotificationHandler> log,
    IDiffService diffService,
    IGuildDiscordNotificationService guildNotifications,
    ConnectedDiscordClientAccessor clientAccessor) : INotificationHandler<GuildSettingsChangedNotification>
{
    /// <inheritdoc />
    public Task HandleAsync(GuildSettingsChangedNotification notification, CancellationToken cancellationToken = default)
    {
        if (diffService.Objects.GetDifferences(notification.Previous, notification.Current).Count == 0)
            return Task.CompletedTask;

        log.LogInformation(
            "Guild {GuildId} settings changed ({Source}). CommandChannel={CommandChannel}, LogChannel={LogChannel}, AdminRole={AdminRole}, ModRole={ModRole}", notification.GuildId,
            notification.Source, notification.Current.CommandChannelId, notification.Current.LogChannelId, notification.Current.AdminRoleId, notification.Current.ModRoleId);

        var client = clientAccessor.Client;
        if (client == null)
            return Task.CompletedTask;

        var body = FormatChangeDetails(notification.Previous, notification.Current, notification.Source);
        return guildNotifications.NotifyGuildLogMessageAsync(client, (ulong)notification.GuildId, "Guild settings updated", body, cancellationToken);
    }

    private static string FormatChangeDetails(DiscordGuildSettings before, DiscordGuildSettings after, string source)
    {
        var sb = new StringBuilder(256);
        sb.AppendFormat(CultureInfo.InvariantCulture, "Source: **{0}**\n", source);
        AppendField(sb, "Command channel", before.CommandChannelId, after.CommandChannelId);
        AppendField(sb, "Log channel", before.LogChannelId, after.LogChannelId);
        AppendField(sb, "Admin role", before.AdminRoleId, after.AdminRoleId);
        AppendField(sb, "Moderator role", before.ModRoleId, after.ModRoleId);
        return sb.ToString();
    }

    private static void AppendField(StringBuilder sb, string label, ulong? before, ulong? after)
    {
        if (before == after)
            return;

        sb.AppendFormat(CultureInfo.InvariantCulture, "**{0}:** `{1}` → `{2}`\n", label, FormatId(before), FormatId(after));
    }

    private static string FormatId(ulong? id) => id is { } v ? v.ToString(CultureInfo.InvariantCulture) : "*(cleared)*";
}