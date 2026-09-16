using Lyo.Exceptions;
using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Google.Youtube.Client;

/// <summary>DI helpers that register <see cref="GoogleYoutubeClient" />.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the YouTube client from a configure callback.</summary>
        public IHttpClientBuilder AddGoogleYoutubeClient(Action<GoogleYoutubeClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            return RegisterInterface(services, services.AddLyoHttpClient<GoogleYoutubeClient, GoogleYoutubeClientOptions>(configure, Create));
        }

        /// <summary>Registers the YouTube client from an options instance.</summary>
        public IHttpClientBuilder AddGoogleYoutubeClient(GoogleYoutubeClientOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            return RegisterInterface(services, services.AddLyoHttpClient<GoogleYoutubeClient, GoogleYoutubeClientOptions>(options, Create));
        }

        /// <summary>Registers the YouTube client from configuration section <paramref name="configSectionName" />.</summary>
        public IHttpClientBuilder AddGoogleYoutubeClientFromConfiguration(IConfiguration configuration, string configSectionName = GoogleYoutubeClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            return RegisterInterface(services, services.AddLyoHttpClient<GoogleYoutubeClient, GoogleYoutubeClientOptions>(configuration, configSectionName, Create));
        }
    }

    private static IHttpClientBuilder RegisterInterface(IServiceCollection services, IHttpClientBuilder builder)
    {
        services.AddTransient<IYoutubeClient>(sp => sp.GetRequiredService<GoogleYoutubeClient>());
        return builder;
    }

    private static GoogleYoutubeClient Create(IServiceProvider provider, HttpClient httpClient, GoogleYoutubeClientOptions options)
        => new(options, provider.GetService<ILoggerFactory>(), httpClient);
}
