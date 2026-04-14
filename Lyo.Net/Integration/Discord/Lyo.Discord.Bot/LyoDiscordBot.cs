using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Lyo.Cache;
using Lyo.Diff;
using Lyo.Discord.Bot.Commands;
using Lyo.Discord.Bot.Commands.Settings;
using Lyo.Discord.Bot.Services;
using Lyo.Discord.Client;
using Lyo.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NativeDiscordClient = DSharpPlus.DiscordClient;

namespace Lyo.Discord.Bot;

/// <summary>Default implementation: Lyo API database sync, slash commands (<c>/settings</c>), and interactivity.</summary>
public class LyoDiscordBot : LyoDiscordBotBase
{
    private readonly ICacheService _cache;
    private readonly IDiffService _diffService;
    private readonly INotificationPublisher _notificationPublisher;

    public LyoDiscordBot(
        LyoDiscordBotOptions options,
        LyoDiscordClient lyoApiClient,
        IGuildDatabaseSyncService sync,
        ILoggerFactory loggerFactory,
        ConnectedDiscordClientAccessor discordClientAccessor,
        IGuildDiscordNotificationService guildNotifications,
        ICacheService cache,
        INotificationPublisher notificationPublisher,
        IDiffService diffService)
        : base(options, lyoApiClient, sync, loggerFactory, discordClientAccessor, diffService, guildNotifications)
    {
        _cache = cache;
        _notificationPublisher = notificationPublisher;
        _diffService = diffService;
    }

    /// <inheritdoc />
    protected override void ConfigureDiscordClient(NativeDiscordClient client)
    {
        base.ConfigureDiscordClient(client);
        client.UseInteractivity(new());
        var services = new ServiceCollection();
        services.AddSingleton(LyoApiClient);
        services.AddSingleton(_cache);
        services.AddSingleton(_notificationPublisher);
        services.AddSingleton(_diffService);
        var slash = client.UseSlashCommands(new() { Services = services.BuildServiceProvider() });
        slash.RegisterCommands<GuildSettingsSlashCommands>();
        SlashCommandErrorResponder.Subscribe(slash, Logger, client, GuildNotifications);
    }
}