using Lyo.Exceptions;
using Lyo.Geolocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Google.Geolocation.Client;

/// <summary>Dependency injection for Google Maps geolocation.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="GoogleMapsClient" /> from configuration (section <see cref="GoogleMapsClientOptions.SectionName" />).</summary>
        public IServiceCollection AddGoogleMapsClientFromConfiguration(IConfiguration configuration, string configSectionName = GoogleMapsClientOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new GoogleMapsClientOptions();
            var section = configuration.GetSection(configSectionName);
            if (section.Exists())
                section.Bind(options);

            return services.AddGoogleMapsClient(options);
        }

        /// <summary>Registers <see cref="GoogleMapsClient" />.</summary>
        public IServiceCollection AddGoogleMapsClient(Action<GoogleMapsClientOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new GoogleMapsClientOptions();
            configure(options);
            return services.AddGoogleMapsClient(options);
        }

        /// <summary>Registers <see cref="GoogleMapsClient" />.</summary>
        public IServiceCollection AddGoogleMapsClient(GoogleMapsClientOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddSingleton(options);
            services.AddSingleton<GoogleMapsClient>(sp => {
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var httpClient = sp.GetService<HttpClient>();
                return new GoogleMapsClient(options, loggerFactory, httpClient);
            });
            return services;
        }

        /// <summary>Registers <see cref="GoogleMapsGeolocationService" /> as <see cref="IGeolocationService" />.</summary>
        public IServiceCollection AddGoogleMapsGeolocationService()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.AddSingleton<IGeolocationService>(sp => new GoogleMapsGeolocationService(sp.GetRequiredService<GoogleMapsClient>()));
            return services;
        }

    }
}
