using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>DI helpers that register <see cref="FantasyFootballClient" /> with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Registers the fantasy football client using configuration binding.</summary>
    public static IHttpClientBuilder AddFantasyFootballClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = FantasyFootballClientOptions.SectionName)
        => services.AddLyoHttpClient<FantasyFootballClient, FantasyFootballClientOptions>(configuration, configSectionName, Create);

    /// <summary>Registers the fantasy football client with inline configuration.</summary>
    public static IHttpClientBuilder AddFantasyFootballClient(this IServiceCollection services, Action<FantasyFootballClientOptions> configure)
        => services.AddLyoHttpClient<FantasyFootballClient, FantasyFootballClientOptions>(configure, Create);

    /// <summary>Registers the fantasy football client with a pre-built options instance.</summary>
    public static IHttpClientBuilder AddFantasyFootballClient(this IServiceCollection services, FantasyFootballClientOptions options)
        => services.AddLyoHttpClient<FantasyFootballClient, FantasyFootballClientOptions>(options, Create);

    private static FantasyFootballClient Create(IServiceProvider provider, HttpClient httpClient, FantasyFootballClientOptions options)
        => new(options, provider.GetService<ILoggerFactory>(), httpClient);
}
