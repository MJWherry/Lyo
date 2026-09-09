using Lyo.Exceptions;
using Lyo.Geolocation;
using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Google.Geolocation.Client;

/// <summary>DI registration for Google Maps geolocation.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="GoogleMapsClient" /> from configuration (section <see cref="GoogleMapsClientOptions.SectionName" />).</summary>
        public IHttpClientBuilder AddGoogleMapsClientFromConfiguration(IConfiguration configuration, string configSectionName = GoogleMapsClientOptions.SectionName)
            => services.AddLyoHttpClient<GoogleMapsClient, GoogleMapsClientOptions>(configuration, configSectionName, Create);

        /// <summary>Adds <see cref="GoogleMapsClient" />.</summary>
        public IHttpClientBuilder AddGoogleMapsClient(Action<GoogleMapsClientOptions> configure)
            => services.AddLyoHttpClient<GoogleMapsClient, GoogleMapsClientOptions>(configure, Create);

        /// <summary>Adds <see cref="GoogleMapsClient" />.</summary>
        public IHttpClientBuilder AddGoogleMapsClient(GoogleMapsClientOptions options)
            => services.AddLyoHttpClient<GoogleMapsClient, GoogleMapsClientOptions>(options, Create);

        /// <summary>Adds <see cref="GoogleMapsGeolocationService" /> as <see cref="IGeolocationService" />.</summary>
        public IServiceCollection AddGoogleMapsGeolocationService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddTransient<IGeolocationService>(sp => new GoogleMapsGeolocationService(sp.GetRequiredService<GoogleMapsClient>()));
            return services;
        }
    }

    private static GoogleMapsClient Create(IServiceProvider provider, HttpClient httpClient, GoogleMapsClientOptions options)
        => new(options, provider.GetService<ILoggerFactory>(), httpClient);
}
