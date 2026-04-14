using DSharpPlus;
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
public abstract class LyoDiscordBotBase
{
    private CancellationToken _runCt;

    protected LyoDiscordBotOptions Options { get; }

    /// <summary>HTTP client for Lyo <c>Discord/*</c> API upserts.</summary>
    protected LyoDiscordClient LyoApiClient { get; }

    protected IGuildDatabaseSyncService Sync { get; }

    protected ILogger Logger { get; }

    /// <summary>Optional service for posting to guild channels (log channel helpers, arbitrary embeds, etc.).</summary>
    protected IGuildDiscordNotificationService? GuildNotifications { get; }

    protected IDiffService DiffService { get; }

    /// <summary>Holds the active gateway client for services that post to guild channels.</summary>
    protected ConnectedDiscordClientAccessor DiscordClientAccessor { get; }

    protected LyoDiscordBotBase(
        LyoDiscordBotOptions options,
        LyoDiscordClient lyoApiClient,
        IGuildDatabaseSyncService sync,
        ILoggerFactory loggerFactory,
        ConnectedDiscordClientAccessor discordClientAccessor,
        IDiffService diffService,
        IGuildDiscordNotificationService? guildNotifications = null)
    {
        Options = options;
        LyoApiClient = lyoApiClient;
        Sync = sync;
        Logger = loggerFactory.CreateLogger(GetType());
        DiscordClientAccessor = discordClientAccessor;
        DiffService = diffService;
        GuildNotifications = guildNotifications;
    }

    /// <summary>Runs until <paramref name="cancellationToken" /> is cancelled: connect, block, disconnect.</summary>
    public virtual async Task RunAsync(CancellationToken cancellationToken)
    {
        _runCt = cancellationToken;
        var cfg = new DiscordConfiguration { Token = Options.Token, TokenType = TokenType.Bot, Intents = Options.Intents };
        ConfigureDiscordConfiguration(cfg);
        using var client = new NativeDiscordClient(cfg);
        DiscordClientAccessor.Client = client;
        try {
            ConfigureDiscordClient(client);
            RegisterDefaultSyncHandlers(client);
            RegisterAdditionalHandlers(client);
            await client.ConnectAsync().ConfigureAwait(false);
            Logger.LogInformation("Discord bot connected. Cancel the host token to stop.");
            try {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                // shutdown
            }

            await client.DisconnectAsync().ConfigureAwait(false);
        }
        finally {
            DiscordClientAccessor.Client = null;
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
            if (GuildNotifications != null && guildId != 0)
                _ = GuildNotifications.NotifyGuildLogErrorAsync(discordClient, guildId, ex, $"Sync: {eventName}", _runCt);
        }
    }
}