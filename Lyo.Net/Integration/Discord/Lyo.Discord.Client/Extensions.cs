using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Discord.Client;

/// <summary>DI registration for <see cref="LyoDiscordClient" />.</summary>
public static class Extensions
{
    /// <summary>Registers the Discord Lyo API client from configuration.</summary>
    public static IHttpClientBuilder AddDiscordClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = LyoDiscordClientOptions.SectionName)
        => services.AddLyoHttpClient<LyoDiscordClient, LyoDiscordClientOptions>(configuration, configSectionName, Create);

    /// <summary>Registers the Discord Lyo API client.</summary>
    public static IHttpClientBuilder AddDiscordClient(this IServiceCollection services, Action<LyoDiscordClientOptions> configure)
        => services.AddLyoHttpClient<LyoDiscordClient, LyoDiscordClientOptions>(configure, Create);

    /// <summary>Registers the Discord Lyo API client.</summary>
    public static IHttpClientBuilder AddDiscordClient(this IServiceCollection services, LyoDiscordClientOptions options)
        => services.AddLyoHttpClient<LyoDiscordClient, LyoDiscordClientOptions>(options, Create);

    private static LyoDiscordClient Create(IServiceProvider provider, HttpClient httpClient, LyoDiscordClientOptions options)
        => new(options, provider.GetService<ILoggerFactory>()?.CreateLogger<LyoDiscordClient>(), httpClient);
}
