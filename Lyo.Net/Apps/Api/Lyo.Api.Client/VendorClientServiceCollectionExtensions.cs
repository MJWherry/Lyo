using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Api.Client;

/// <summary>
/// Obsolete forwards for vendor clients. New registrations should call <see cref="Lyo.Http.Client.Extensions.AddLyoHttpClient{TClient,TOptions}(IServiceCollection, Action{TOptions}?, Func{IServiceProvider, HttpClient, TOptions, TClient}?, string?)" />
/// and return <see cref="IHttpClientBuilder" />.
/// </summary>
public static class VendorClientServiceCollectionExtensions
{
    /// <param name="services">Service collection to configure.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Obsolete: registers via <c>IHttpClientFactory</c> and returns the collection (not the builder). Prefer <c>AddLyoHttpClient</c>.</summary>
        [Obsolete("Use services.AddLyoHttpClient<TClient, TOptions>(configuration, sectionName, factory) which returns IHttpClientBuilder.")]
        public IServiceCollection AddLyoApiClient<TClient, TOptions>(IConfiguration configuration, string configSectionName, Func<IServiceProvider, TOptions, TClient> factory)
            where TClient : class
            where TOptions : LyoHttpClientOptions, new()
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(factory);
            services.AddLyoHttpClient<TClient, TOptions>(configuration, configSectionName, (sp, http, options) => Adapt(factory, sp, http, options));
            return services;
        }

        /// <summary>Obsolete: prefer <c>AddLyoHttpClient</c>.</summary>
        [Obsolete("Use services.AddLyoHttpClient<TClient, TOptions>(configure, factory) which returns IHttpClientBuilder.")]
        public IServiceCollection AddLyoApiClient<TClient, TOptions>(Action<TOptions> configure, Func<IServiceProvider, TOptions, TClient> factory)
            where TClient : class
            where TOptions : LyoHttpClientOptions, new()
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(factory);
            services.AddLyoHttpClient<TClient, TOptions>(configure, (sp, http, options) => Adapt(factory, sp, http, options));
            return services;
        }

        /// <summary>Obsolete: prefer <c>AddLyoHttpClient</c>.</summary>
        [Obsolete("Use services.AddLyoHttpClient<TClient, TOptions>(options, factory) which returns IHttpClientBuilder.")]
        public IServiceCollection AddLyoApiClient<TClient, TOptions>(TOptions options, Func<IServiceProvider, TOptions, TClient> factory)
            where TClient : class
            where TOptions : LyoHttpClientOptions, new()
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(factory);
            services.AddLyoHttpClient<TClient, TOptions>(options, (sp, http, optionsValue) => Adapt(factory, sp, http, optionsValue));
            return services;
        }
    }

    private static TClient Adapt<TClient, TOptions>(Func<IServiceProvider, TOptions, TClient> factory, IServiceProvider sp, HttpClient http, TOptions options)
        where TClient : class
        where TOptions : LyoHttpClientOptions, new()
    {
        // Old factories called GetService<HttpClient>(). Prefer the factory-created client by putting it in a tiny wrapper scope... we pass http via a holder.
        var holder = new HttpClientHolder(http);
        var wrapped = new HttpClientOverrideServiceProvider(sp, holder);
        return factory(wrapped, options);
    }

    private sealed class HttpClientHolder(HttpClient client)
    {
        public HttpClient Client { get; } = client;
    }

    private sealed class HttpClientOverrideServiceProvider(IServiceProvider inner, HttpClientHolder holder) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(HttpClient))
                return holder.Client;

            return inner.GetService(serviceType);
        }
    }
}
