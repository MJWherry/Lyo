using Lyo.Exceptions;
using Lyo.ShortUrl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.ShortUrl;

/// <summary>DI helpers for URL shortener service registration.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers a URL shortener service to the service collection.</summary>
        /// <typeparam name="TService">Concrete URL-shortener service type.</typeparam>
        /// <typeparam name="TOptions">Options type for the URL shortener.</typeparam>
        /// <param name="configure">Callback that fills the options object.</param>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddShortUrlService<TService, TOptions>(Action<TOptions>? configure = null)
            where TService : class, IShortUrlService where TOptions : ShortUrlServiceOptions, new()
        {
            if (configure != null)
                services.Configure(configure);

            services.AddSingleton<IShortUrlService, TService>();
            return services;
        }

        /// <summary>Registers a URL shortener service to the service collection with explicit options.</summary>
        /// <typeparam name="TService">Concrete URL-shortener service type.</typeparam>
        /// <param name="options">Options for the URL shortener.</param>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddShortUrlService<TService>(ShortUrlServiceOptions options)
            where TService : class, IShortUrlService
        {
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddSingleton<IShortUrlService, TService>();
            return services;
        }

        /// <summary>Registers short-URL generator service to the service collection.</summary>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddShortUrlGenerator()
        {
            services.AddSingleton<IShortUrlGenerator, ShortUrlGenerator>();
            return services;
        }

        /// <summary>Registers URL-shortener service to the service collection.</summary>
        /// <param name="configure">Optional callback that fills options.</param>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddShortUrl(Action<ShortUrlServiceOptions>? configure = null)
        {
            var options = new ShortUrlServiceOptions();
            configure?.Invoke(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddShortUrlGenerator();
            services.AddSingleton<IShortUrlService, ShortUrlService>();
            return services;
        }

        /// <summary>Registers URL-shortener service to the service collection with explicit options.</summary>
        /// <param name="options">Options for the URL shortener.</param>
        /// <returns>Same collection so registration can be chained.</returns>
        public IServiceCollection AddShortUrl(ShortUrlServiceOptions options)
        {
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddShortUrlGenerator();
            services.AddSingleton<IShortUrlService, ShortUrlService>();
            return services;
        }

        /// <summary>Registers URL-shortener service to the service collection using configuration binding.</summary>
        /// <param name="configuration">Host configuration (for example builder.Configuration).</param>
        /// <param name="configSectionName">Configuration section name; defaults to "ShortUrlOptions". Bound from IConfiguration when that is registered.</param>
        /// <returns>Same collection so registration can be chained.</returns>
        /// <remarks>
        /// <para>When IConfiguration is in the service collection, this overload binds from it. Otherwise the options keep their defaults.</para>
        /// <para>Sample appsettings.json fragment:</para>
        /// <code>
        /// {
        ///   "ShortUrlOptions": {
        ///     "BaseUrl": "https://short.ly",
        ///     "DefaultExpirationDays": 30,
        ///     "MaxAliasLength": 50,
        ///     "MinAliasLength": 3,
        ///     "AllowCustomAliases": true,
        ///     "EnableMetrics": false,
        ///     "EnforceHttps": false
        ///   }
        /// }
        /// </code>
        /// </remarks>
        public IServiceCollection AddShortUrlFromConfiguration(IConfiguration configuration, string configSectionName = ShortUrlServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            if (!services.Any(s => s.ServiceType == typeof(ShortUrlServiceOptions))) {
                var options = new ShortUrlServiceOptions();
                configuration.GetSection(configSectionName).Bind(options);
                options.Validate();
                services.AddSingleton(options);
            }

            services.AddShortUrlGenerator();
            services.AddSingleton<IShortUrlService, ShortUrlService>();
            return services;
        }
    }
}