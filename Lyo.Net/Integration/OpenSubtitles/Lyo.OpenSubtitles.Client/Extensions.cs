using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.OpenSubtitles.Client;

/// <summary>DI helpers that register <see cref="OpenSubtitlesClient" />.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the client from configuration section <see cref="OpenSubtitlesClientOptions.SectionName" />.</summary>
        public IHttpClientBuilder AddOpenSubtitlesClientFromConfiguration(IConfiguration configuration, string configSectionName = OpenSubtitlesClientOptions.SectionName)
            => services.AddLyoHttpClient<OpenSubtitlesClient, OpenSubtitlesClientOptions>(configuration, configSectionName, Create);

        /// <summary>Registers the client and applies <paramref name="configure" /> before validation.</summary>
        public IHttpClientBuilder AddOpenSubtitlesClient(Action<OpenSubtitlesClientOptions> configure)
            => services.AddLyoHttpClient<OpenSubtitlesClient, OpenSubtitlesClientOptions>(configure, Create);

        /// <summary>Registers the client from a ready options instance.</summary>
        public IHttpClientBuilder AddOpenSubtitlesClient(OpenSubtitlesClientOptions options)
            => services.AddLyoHttpClient<OpenSubtitlesClient, OpenSubtitlesClientOptions>(options, Create);
    }

    private static OpenSubtitlesClient Create(IServiceProvider provider, HttpClient httpClient, OpenSubtitlesClientOptions options)
        => new(options, provider.GetService<ILoggerFactory>(), httpClient);
}
