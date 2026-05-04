using DSharpPlus;
using DSharpPlus.Entities;
using Lyo.Diff;
using Lyo.Discord.Bot.Services;
using Lyo.Discord.Client;
using Microsoft.Extensions.Logging;
using NativeDiscordClient = DSharpPlus.DiscordClient;

namespace Lyo.Discord.Bot;

/// <summary>
/// Base Discord bot: connects with DSharpPlus, runs optional database sync via <see cref="IGuildDatabaseSyncService" />, and exposes hooks for derived bots (slash/commands,
/// interactions, extra events).
/// </summary>
public abstract class LyoDiscordBotBase : ILyoDiscordBotGateway
{
    private NativeDiscordClient? _gatewayClient;
    private CancellationToken _runCt;

    protected LyoDiscordBotOptions Options { get; }

    /// <summary>HTTP client for Lyo <c>Discord/*</c> API upserts.</summary>
    protected LyoDiscordClient LyoApiClient { get; }

    protected IGuildDatabaseSyncService Sync { get; }

    protected ILogger Logger { get; }

    /// <summary>Optional service for posting to guild channels (log channel helpers, arbitrary embeds, etc.).</summary>
    protected IGuildDiscordNotificationService? GuildNotifications { get; }

    protected IDiffService DiffService { get; }

    protected LyoDiscordBotBase(
        LyoDiscordBotOptions options,
        LyoDiscordClient lyoApiClient,
        IGuildDatabaseSyncService sync,
        ILoggerFactory loggerFactory,
        IDiffService diffService,
        IGuildDiscordNotificationService? guildNotifications = null)
    {
        Options = options;
        LyoApiClient = lyoApiClient;
        Sync = sync;
        Logger = loggerFactory.CreateLogger(GetType());
        DiffService = diffService;
        GuildNotifications = guildNotifications;
    }

    /// <inheritdoc />
    public bool IsConnected => _gatewayClient != null;

    /// <inheritdoc />
    public Task NotifyGuildLogMessageAsync(ulong guildId, string title, string body, CancellationToken ct = default)
    {
        if (GuildNotifications == null || _gatewayClient == null)
            return Task.CompletedTask;

        return GuildNotifications.NotifyGuildLogMessageAsync(_gatewayClient, guildId, title, body, ct);
    }

    /// <inheritdoc />
    public Task NotifyGuildLogErrorAsync(ulong guildId, Exception exception, string context, CancellationToken ct = default)
    {
        if (GuildNotifications == null || _gatewayClient == null)
            return Task.CompletedTask;

        return GuildNotifications.NotifyGuildLogErrorAsync(_gatewayClient, guildId, exception, context, ct);
    }

    /// <inheritdoc />
    public Task<bool> TrySendEmbedAsync(ulong guildId, ulong channelId, DiscordEmbed embed, CancellationToken ct = default)
    {
        if (GuildNotifications == null || _gatewayClient == null)
            return Task.FromResult(false);

        return GuildNotifications.TrySendEmbedAsync(_gatewayClient, guildId, channelId, embed, ct);
    }

    /// <inheritdoc />
    public Task<bool> TrySendMessageAsync(ulong guildId, ulong channelId, string? content = null, DiscordEmbed? embed = null, CancellationToken ct = default)
    {
        if (GuildNotifications == null || _gatewayClient == null)
            return Task.FromResult(false);

        return GuildNotifications.TrySendMessageAsync(_gatewayClient, guildId, channelId, content, embed, ct);
    }

    /// <inheritdoc />
    public Task<bool> TrySendMessageAsync(ulong guildId, ulong channelId, DiscordMessageBuilder message, CancellationToken ct = default)
    {
        if (GuildNotifications == null || _gatewayClient == null)
            return Task.FromResult(false);

        return GuildNotifications.TrySendMessageAsync(_gatewayClient, guildId, channelId, message, ct);
    }

    /// <inheritdoc />
    public Task<bool> TrySendEmbedToGuildLogChannelAsync(ulong guildId, DiscordEmbed embed, CancellationToken ct = default)
    {
        if (GuildNotifications == null || _gatewayClient == null)
            return Task.FromResult(false);

        return GuildNotifications.TrySendEmbedToGuildLogChannelAsync(_gatewayClient, guildId, embed, ct);
    }

    /// <inheritdoc />
    public Task<bool> TrySendMessageToGuildLogChannelAsync(ulong guildId, string? content = null, DiscordEmbed? embed = null, CancellationToken ct = default)
    {
        if (GuildNotifications == null || _gatewayClient == null)
            return Task.FromResult(false);

        return GuildNotifications.TrySendMessageToGuildLogChannelAsync(_gatewayClient, guildId, content, embed, ct);
    }

    /// <inheritdoc />
    public Task<bool> TrySendMessageToGuildLogChannelAsync(ulong guildId, DiscordMessageBuilder message, CancellationToken ct = default)
    {
        if (GuildNotifications == null || _gatewayClient == null)
            return Task.FromResult(false);

        return GuildNotifications.TrySendMessageToGuildLogChannelAsync(_gatewayClient, guildId, message, ct);
    }

    /// <summary>Runs until <paramref name="ct" /> is cancelled: connect, block, disconnect.</summary>
    public virtual async Task RunAsync(CancellationToken ct)
    {
        _runCt = ct;
        var cfg = new DiscordConfiguration { Token = Options.Token, TokenType = TokenType.Bot, Intents = Options.Intents };
        ConfigureDiscordConfiguration(cfg);
        using var client = new NativeDiscordClient(cfg);
        _gatewayClient = client;
        try {
            ConfigureDiscordClient(client);
            RegisterDefaultSyncHandlers(client);
            RegisterAdditionalHandlers(client);
            await client.ConnectAsync().ConfigureAwait(false);
            Logger.LogInformation("Discord bot connected. Cancel the host token to stop.");
            try {
                await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                // shutdown
            }

            await client.DisconnectAsync().ConfigureAwait(false);
        }
        finally {
            _gatewayClient = null;
        }
    }

    /// <summary>Override to adjust gateway configuration before the native client is constructed.</summary>
    protected virtual void ConfigureDiscordConfiguration(DiscordConfiguration configuration) { }

    /// <summary>
    /// Override to register extensions on the DSharpPlus client (e.g. CommandsNext, SlashCommands, Interactivity) after construction and before
    /// <see cref="RegisterDefaultSyncHandlers" />.
    /// </summary>
    protected virtual void ConfigureDiscordClient(NativeDiscordClient client) { }

    /// <summary>Subscribe to sync-related gateway events. Override to add handlers; call <c>base.RegisterDefaultSyncHandlers(client)</c> to keep DB sync.</summary>
    protected virtual void RegisterDefaultSyncHandlers(NativeDiscordClient client)
    {
        client.GuildAvailable += (_, e) => SafeSync(client, "GuildAvailable", e.Guild.Id, () => Sync.SyncGuildFullAsync(e.Guild, _runCt));
        client.GuildCreated += (_, e) => SafeSync(client, "GuildCreated", e.Guild.Id, () => Sync.SyncGuildFullAsync(e.Guild, _runCt));
        client.GuildDownloadCompleted += (_, e) => SafeSync(
            client, "GuildDownloadCompleted", 0, async () => {
                foreach (var g in e.Guilds.Values)
                    await SafeSync(client, "GuildDownloadCompleted", g.Id, () => Sync.SyncGuildFullAsync(g, _runCt)).ConfigureAwait(false);
            });

        client.GuildUpdated += (_, e) => SafeSync(
            client, "GuildUpdated", e.GuildAfter?.Id ?? 0, () => e.GuildAfter != null ? Sync.SyncGuildMetadataAsync(e.GuildAfter, _runCt) : Task.CompletedTask);

        client.ChannelCreated += (_, e) => SafeSync(client, "ChannelCreated", e.Channel.Guild?.Id ?? 0, () => Sync.SyncChannelAsync(e.Channel, _runCt));
        client.ChannelUpdated += (_, e) => SafeSync(client, "ChannelUpdated", e.ChannelAfter.Guild?.Id ?? 0, () => Sync.SyncChannelAsync(e.ChannelAfter, _runCt));
        client.GuildMemberAdded += (_, e) => SafeSync(client, "GuildMemberAdded", e.Member.Guild?.Id ?? 0, () => Sync.SyncGuildMemberAsync(e.Member, _runCt));
        client.GuildMemberUpdated += (_, e) => SafeSync(client, "GuildMemberUpdated", e.MemberAfter.Guild?.Id ?? 0, () => Sync.SyncGuildMemberAsync(e.MemberAfter, _runCt));
        client.GuildEmojisUpdated += (_, e) => SafeSync(client, "GuildEmojisUpdated", e.Guild.Id, () => Sync.SyncGuildEmojisAsync(e.Guild, _runCt));
    }

    /// <summary>Override to subscribe to non-sync events or additional handlers.</summary>
    protected virtual void RegisterAdditionalHandlers(NativeDiscordClient client) { }

    private async Task SafeSync(NativeDiscordClient discordClient, string eventName, ulong guildId, Func<Task> work)
    {
        try {
            await work().ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Discord {EventName} sync handler failed", eventName);
            if (guildId != 0)
                _ = NotifyGuildLogErrorAsync(guildId, ex, $"Sync: {eventName}", _runCt);
        }
    }
}