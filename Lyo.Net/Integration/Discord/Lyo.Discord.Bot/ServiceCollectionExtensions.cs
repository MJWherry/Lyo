using Lyo.Discord.Bot.Services;
using Lyo.Discord.Client;
using Lyo.Exceptions;
using Lyo.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Discord.Bot;

/// <summary>Registers <see cref="LyoDiscordBotOptions" />, <see cref="IGuildDatabaseSyncService" />, <see cref="LyoDiscordClient" />, and <typeparamref name="TBot" />.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Binds <see cref="LyoDiscordBotOptions" /> from configuration, registers <see cref="LyoDiscordClient" />, sync service, and the bot implementation (singleton).</summary>
    public static IServiceCollection AddLyoDiscordBot<TBot>(this IServiceCollection services, IConfiguration configuration, string sectionName = LyoDiscordBotOptions.SectionName)
        where TBot : LyoDiscordBotBase
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        services.Configure<LyoDiscordBotOptions>(configuration.GetSection(sectionName));
        services.AddSingleton(p => p.GetRequiredService<IOptions<LyoDiscordBotOptions>>().Value);
        services.AddSingleton<ConnectedDiscordClientAccessor>();
        services.AddSingleton<IGuildDiscordNotificationService, GuildDiscordNotificationService>();
        services.AddLyoNotification();
        services.AddSingleton<INotificationHandler<GuildSettingsChangedNotification>, GuildSettingsChangedNotificationHandler>();
        services.AddSingleton<IGuildDatabaseSyncService, GuildDatabaseSyncService>();
        services.AddSingleton<TBot>();
        services.AddSingleton<LyoDiscordBotBase>(sp => sp.GetRequiredService<TBot>());
        services.AddSingleton<LyoDiscordClient>(sp => {
            var opts = sp.GetRequiredService<IOptions<LyoDiscordBotOptions>>().Value;
            var url = string.IsNullOrWhiteSpace(opts.LyoApiBaseUrl) ? "http://localhost:5092/" : opts.LyoApiBaseUrl.TrimEnd('/') + "/";
            var lf = sp.GetService<ILoggerFactory>();
            return new(new() { Url = url }, lf?.CreateLogger<LyoDiscordClient>());
        });

        return services;
    }
}