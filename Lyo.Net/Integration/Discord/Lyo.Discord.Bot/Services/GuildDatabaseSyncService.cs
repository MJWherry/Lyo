using System.Globalization;
using DSharpPlus;
using DSharpPlus.Entities;
using Lyo.Api.Models.Common.Request;
using Lyo.Cache;
using Lyo.Common;
using Lyo.Diff;
using Lyo.Discord.Client;
using Lyo.Discord.Models;
using Lyo.Discord.Models.Request;
using Lyo.Notification;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Lyo.Discord.Bot.Services;

/// <summary>Default implementation: maps DSharpPlus entities to Lyo API upserts (bulk where supported).</summary>
public sealed class GuildDatabaseSyncService(
    LyoDiscordClient lyoDiscordClient,
    ICacheService cache,
    IDiffService diffService,
    INotificationPublisher notificationPublisher,
    ILogger<GuildDatabaseSyncService> logger) : IGuildDatabaseSyncService
{
    /// <summary>Epoch and shift for snowflake IDs from the chat API (2015-01-01 UTC origin, 22-bit timestamp field).</summary>
    private static readonly SnowflakeLayout ChatApiSnowflakeLayout = new(22, 1420070400000L);

    /// <inheritdoc />
    public async Task SyncGuildFullAsync(DiscordGuild guild, CancellationToken cancellationToken = default)
    {
        var gid = new Snowflake(guild.Id).ToInt64();
        using var scope = logger.BeginScope("GuildSync {GuildId}", gid);
        logger.LogInformation("Starting full guild sync for {GuildName} ({GuildId})", guild.Name, gid);
        var ownerId = guild.OwnerId != 0 ? new Snowflake(guild.OwnerId).ToInt64() : 0L;
        var joined = guild.JoinedAt == default ? DateTime.UtcNow : guild.JoinedAt.UtcDateTime;
        if (ownerId != 0) {
            var ownerMember = await TryResolveOwnerMemberAsync(guild).ConfigureAwait(false);
            var ownerReq = ownerMember != null ? MapUser(ownerMember) : MapUserStub(ownerId);
            logger.LogInformation("Upserting owner user {OwnerId} before guild row", ownerId);
            var ownerRes = await lyoDiscordClient.Users.UpsertAsync(ownerReq, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Owner user upsert {OwnerId} -> {Result}", ownerId, ownerRes.Result);
        }

        var guildReq = new DiscordGuildReq {
            Id = gid,
            OwnerId = ownerId,
            Name = guild.Name,
            Description = guild.Description,
            MemberCount = guild.MemberCount,
            CurrentSubscriptionCount = guild.PremiumSubscriptionCount ?? 0,
            IsLarge = guild.IsLarge,
            IsNSFW = guild.IsNSFW,
            IsUnavailable = guild.IsUnavailable,
            GuildCreatedDate = guild.CreationTimestamp.UtcDateTime,
            JoinedDate = joined
        };

        var gRes = await lyoDiscordClient.Guilds.UpsertAsync(guildReq, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Guild upsert {GuildId} -> {Result}", gid, gRes.Result);
        try {
            var settings = await lyoDiscordClient.Guilds.GetSettingsAsync(gid, cancellationToken).ConfigureAwait(false) ?? new DiscordGuildSettings();
            settings.NormalizeForRead();
            cache.SetGuildSettings(diffService, notificationPublisher, gid, settings, "guild-sync-full");
            logger.LogDebug(
                "Guild {GuildId} settings: CommandChannel={CommandChannelId}, LogChannel={LogChannelId}, AdminRole={AdminRoleId}, ModRole={ModRoleId}", gid,
                settings.CommandChannelId, settings.LogChannelId, settings.AdminRoleId, settings.ModRoleId);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to load guild settings for {GuildId}", gid);
        }

        var channelUpserts = guild.Channels.Values.Select(ch => {
                var cReq = MapChannel(ch, gid);
                return new UpsertRequest<DiscordChannelReq>(cReq, "Id", cReq.Id);
            })
            .ToList();

        if (channelUpserts.Count > 0) {
            logger.LogInformation("Bulk upserting {Count} channels for guild {GuildId}", channelUpserts.Count, gid);
            var chBulk = await lyoDiscordClient.Channels.UpsertBulkAsync(channelUpserts, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Channels bulk guild {GuildId}: created={Created} updated={Updated} noChange={NoChange} failed={Failed}", gid, chBulk.CreatedCount, chBulk.UpdatedCount,
                chBulk.NoChangeCount, chBulk.FailedCount);
        }

        var roleUpserts = guild.Roles.Values.Select(r => {
                var rReq = MapRole(r, gid);
                return new UpsertRequest<DiscordRoleReq>(rReq, "Id", rReq.Id);
            })
            .ToList();

        if (roleUpserts.Count > 0) {
            logger.LogInformation("Bulk upserting {Count} roles for guild {GuildId}", roleUpserts.Count, gid);
            var roBulk = await lyoDiscordClient.Roles.UpsertBulkAsync(roleUpserts, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Roles bulk guild {GuildId}: created={Created} updated={Updated} noChange={NoChange} failed={Failed}", gid, roBulk.CreatedCount, roBulk.UpdatedCount,
                roBulk.NoChangeCount, roBulk.FailedCount);
        }

        IReadOnlyList<DiscordEmoji> emojisFromApi;
        try {
            emojisFromApi = await guild.GetEmojisAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "GetEmojisAsync failed for guild {GuildId}; using cached Emojis if any", gid);
            emojisFromApi = guild.Emojis.Values.ToList();
        }

        var emojiUpserts = emojisFromApi.Select(e => {
                var eReq = MapEmoji(e, gid);
                return new UpsertRequest<DiscordEmojiReq>(eReq, "Id", eReq.Id);
            })
            .ToList();

        if (emojiUpserts.Count > 0) {
            logger.LogInformation("Bulk upserting {Count} emojis for guild {GuildId}", emojiUpserts.Count, gid);
            var emBulk = await lyoDiscordClient.Emojis.UpsertBulkAsync(emojiUpserts, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Emojis bulk guild {GuildId}: created={Created} updated={Updated} noChange={NoChange} failed={Failed}", gid, emBulk.CreatedCount, emBulk.UpdatedCount,
                emBulk.NoChangeCount, emBulk.FailedCount);
        }

        var userUpserts = new List<UpsertRequest<DiscordUserReq>>();
        var memberUpserts = new List<UpsertRequest<DiscordMemberReq>>();
        foreach (var m in guild.Members.Values) {
            var uid = new Snowflake(m.Id).ToInt64();
            var userReq = MapUser(m);
            userUpserts.Add(new(userReq, "Id", userReq.Id));
            var memberReq = new DiscordMemberReq {
                UserId = uid,
                GuildId = gid,
                JoinedAtUtc = m.JoinedAt == default ? null : m.JoinedAt.UtcDateTime,
                Nickname = m.Nickname,
                ExtraJson = null
            };

            var memberQuery = new GroupClause(
                GroupOperatorEnum.And, null, new ConditionClause("UserId", ComparisonOperatorEnum.Equals, memberReq.UserId),
                new ConditionClause("GuildId", ComparisonOperatorEnum.Equals, memberReq.GuildId));

            memberUpserts.Add(new(memberReq, memberQuery));
        }

        if (userUpserts.Count > 0) {
            logger.LogInformation("Bulk upserting {Count} users for guild {GuildId}", userUpserts.Count, gid);
            var uBulk = await lyoDiscordClient.Users.UpsertBulkAsync(userUpserts, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Users bulk guild {GuildId}: created={Created} updated={Updated} noChange={NoChange} failed={Failed}", gid, uBulk.CreatedCount, uBulk.UpdatedCount,
                uBulk.NoChangeCount, uBulk.FailedCount);
        }

        if (memberUpserts.Count > 0) {
            logger.LogInformation("Bulk upserting {Count} members for guild {GuildId}", memberUpserts.Count, gid);
            var mBulk = await lyoDiscordClient.Members.UpsertBulkAsync(memberUpserts, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Members bulk guild {GuildId}: created={Created} updated={Updated} noChange={NoChange} failed={Failed}", gid, mBulk.CreatedCount, mBulk.UpdatedCount,
                mBulk.NoChangeCount, mBulk.FailedCount);
        }

        logger.LogInformation("Finished full guild sync for {GuildId}", gid);
    }

    /// <inheritdoc />
    public async Task SyncGuildMetadataAsync(DiscordGuild guild, CancellationToken cancellationToken = default)
    {
        var gid = new Snowflake(guild.Id).ToInt64();
        var ownerId = guild.OwnerId != 0 ? new Snowflake(guild.OwnerId).ToInt64() : 0L;
        var joined = guild.JoinedAt == default ? DateTime.UtcNow : guild.JoinedAt.UtcDateTime;
        var guildReq = new DiscordGuildReq {
            Id = gid,
            OwnerId = ownerId,
            Name = guild.Name,
            Description = guild.Description,
            MemberCount = guild.MemberCount,
            CurrentSubscriptionCount = 0,
            IsLarge = guild.IsLarge,
            IsNSFW = guild.NsfwLevel != NsfwLevel.Default,
            IsUnavailable = guild.IsUnavailable,
            GuildCreatedDate = guild.CreationTimestamp.UtcDateTime,
            JoinedDate = joined
        };

        logger.LogInformation("Upserting guild metadata for {GuildId}", gid);
        var gRes = await lyoDiscordClient.Guilds.UpsertAsync(guildReq, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Guild metadata upsert {GuildId} -> {Result}", gid, gRes.Result);
        try {
            var settings = await lyoDiscordClient.Guilds.GetSettingsAsync(gid, cancellationToken).ConfigureAwait(false) ?? new DiscordGuildSettings();
            settings.NormalizeForRead();
            cache.SetGuildSettings(diffService, notificationPublisher, gid, settings, "guild-sync-metadata");
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to refresh guild settings cache for {GuildId}", gid);
        }
    }

    /// <inheritdoc />
    public async Task SyncChannelAsync(DiscordChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel.Guild == null) {
            logger.LogDebug("Skipping channel sync (no guild): {ChannelId}", channel.Id);
            return;
        }

        var gid = new Snowflake(channel.Guild.Id).ToInt64();
        var cReq = MapChannel(channel, gid);
        logger.LogInformation("Upserting channel {ChannelId} in guild {GuildId}", cReq.Id, gid);
        var res = await lyoDiscordClient.Channels.UpsertAsync(cReq, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Channel upsert {ChannelId} -> {Result}", cReq.Id, res.Result);
    }

    /// <inheritdoc />
    public async Task SyncGuildMemberAsync(DiscordMember member, CancellationToken cancellationToken = default)
    {
        if (member.Guild == null) {
            logger.LogDebug("Skipping member sync (no guild) for user {UserId}", member.Id);
            return;
        }

        var gid = new Snowflake(member.Guild.Id).ToInt64();
        var uid = new Snowflake(member.Id).ToInt64();
        var userReq = MapUser(member);
        logger.LogInformation("Upserting user {UserId} (member event)", uid);
        var uRes = await lyoDiscordClient.Users.UpsertAsync(userReq, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("User upsert {UserId} -> {Result}", uid, uRes.Result);
        var memberReq = new DiscordMemberReq {
            UserId = uid,
            GuildId = gid,
            JoinedAtUtc = member.JoinedAt == default ? null : member.JoinedAt.UtcDateTime,
            Nickname = member.Nickname,
            ExtraJson = null
        };

        var memberQuery = new GroupClause(
            GroupOperatorEnum.And, null, new ConditionClause("UserId", ComparisonOperatorEnum.Equals, memberReq.UserId),
            new ConditionClause("GuildId", ComparisonOperatorEnum.Equals, memberReq.GuildId));

        logger.LogInformation("Upserting member ({UserId},{GuildId})", uid, gid);
        var mRes = await lyoDiscordClient.Members.UpsertAsync(memberReq, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Member upsert ({UserId},{GuildId}) -> {Result}", uid, gid, mRes.Result);
    }

    /// <inheritdoc />
    public Task SyncGuildEmojisAsync(DiscordGuild guild, CancellationToken cancellationToken = default) => SyncGuildEmojisCoreAsync(guild, cancellationToken);

    private async Task SyncGuildEmojisCoreAsync(DiscordGuild guild, CancellationToken cancellationToken)
    {
        var gid = new Snowflake(guild.Id).ToInt64();
        IReadOnlyList<DiscordEmoji> emojisFromApi;
        try {
            emojisFromApi = await guild.GetEmojisAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "GetEmojisAsync failed for guild {GuildId}; using cache", gid);
            emojisFromApi = guild.Emojis.Values.ToList();
        }

        var emojiUpserts = emojisFromApi.Select(e => {
                var eReq = MapEmoji(e, gid);
                return new UpsertRequest<DiscordEmojiReq>(eReq, "Id", eReq.Id);
            })
            .ToList();

        if (emojiUpserts.Count == 0) {
            logger.LogDebug("No emojis to upsert for guild {GuildId}", gid);
            return;
        }

        logger.LogInformation("Bulk upserting {Count} emojis (emoji event) for guild {GuildId}", emojiUpserts.Count, gid);
        var emBulk = await lyoDiscordClient.Emojis.UpsertBulkAsync(emojiUpserts, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Emojis bulk guild {GuildId}: created={Created} updated={Updated} noChange={NoChange} failed={Failed}", gid, emBulk.CreatedCount, emBulk.UpdatedCount,
            emBulk.NoChangeCount, emBulk.FailedCount);
    }

    private static DiscordChannelReq MapChannel(DiscordChannel ch, long guildId)
        => new() {
            Id = new Snowflake(ch.Id).ToInt64(),
            GuildId = guildId,
            Name = ch.Name,
            Topic = ch.Topic,
            ChannelType = ch.Type.ToString(),
            IsCategory = ch.Type == ChannelType.Category,
            IsNSFW = ch.IsNSFW,
            IsPrivate = IsPrivateChannel(ch),
            IsThread = ch.IsThread,
            Position = ch.Position,
            ParentId = ch.ParentId is ulong pid ? new Snowflake(pid).ToInt64() : null,
            ChannelCreated = ch.CreationTimestamp.UtcDateTime
        };

    private static bool IsPrivateChannel(DiscordChannel ch)
    {
        if (ch.Type == ChannelType.Voice)
            return false;

        if (ch.Guild == null)
            return false;

        foreach (var ov in ch.PermissionOverwrites) {
            if (ov.Type == OverwriteType.Role && ov.Id == ch.Guild.Id && (ov.Denied & Permissions.AccessChannels) != 0)
                return true;
        }

        return false;
    }

    private static async Task<DiscordMember?> TryResolveOwnerMemberAsync(DiscordGuild guild)
    {
        if (guild.OwnerId == 0)
            return null;

        if (guild.Members.TryGetValue(guild.OwnerId, out var fromMembers))
            return fromMembers;

        if (guild.Owner != null)
            return guild.Owner;

        try {
            return await guild.GetMemberAsync(guild.OwnerId).ConfigureAwait(false);
        }
        catch {
            return null;
        }
    }

    private static DiscordUserReq MapUserStub(long userId)
        => new() {
            Id = userId,
            Username = "(pending)",
            Discriminator = 0,
            Email = null,
            Locale = null,
            IsVerified = null,
            IsBot = false,
            IsSystem = null,
            IsMfaEnabled = null,
            PremiumLevel = null,
            UserCreatedDate = Snowflake.FromInt64(userId).GetUtcDateTime(ChatApiSnowflakeLayout)
        };

    private static DiscordRoleReq MapRole(DiscordRole r, long guildId)
    {
        var rid = new Snowflake(r.Id).ToInt64();
        return new() {
            Id = rid,
            GuildId = guildId,
            EmojiId = null,
            Name = r.Name ?? "(unknown)",
            Icon = r.IconHash,
            Color = $"#{r.Color.Value:X6}",
            IsHoisted = r.IsHoisted,
            IsManaged = r.IsManaged,
            IsMentionable = r.IsMentionable,
            Position = r.Position,
            RoleCreatedDate = Snowflake.FromInt64(rid).GetUtcDateTime(ChatApiSnowflakeLayout)
        };
    }

    private static DiscordEmojiReq MapEmoji(DiscordEmoji e, long guildId)
        => new() {
            Id = new Snowflake(e.Id).ToInt64(),
            GuildId = guildId,
            Name = string.IsNullOrEmpty(e.Name) ? "(unknown)" : e.Name,
            Url = e.Id != 0 ? e.Url : null,
            IsAnimated = e.IsAnimated,
            IsAvailable = e.IsAvailable,
            IsManaged = e.IsManaged,
            RequiresColons = e.RequiresColons,
            EmojiCreatedDate = e.CreationTimestamp.UtcDateTime
        };

    private static DiscordUserReq MapUser(DiscordMember m)
    {
        var disc = 0;
        if (!string.IsNullOrEmpty(m.Discriminator) && int.TryParse(m.Discriminator, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d))
            disc = d;

        return new() {
            Id = new Snowflake(m.Id).ToInt64(),
            Username = m.Username,
            Discriminator = disc,
            Email = null,
            Locale = m.Locale,
            IsVerified = m.Verified,
            IsBot = m.IsBot,
            IsSystem = m.IsSystem,
            IsMfaEnabled = m.MfaEnabled,
            PremiumLevel = null,
            UserCreatedDate = m.CreationTimestamp.UtcDateTime
        };
    }
}