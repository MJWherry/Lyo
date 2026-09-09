using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Endato.Client;

/// <summary>DI helpers that register Endato client with dependency injection.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers Endato client using configuration binding.</summary>
        public IHttpClientBuilder AddEndatoClientFromConfiguration(IConfiguration configuration, string configSectionName = EndatoClientOptions.SectionName)
            => services.AddLyoHttpClient<EndatoClient, EndatoClientOptions>(configuration, configSectionName, Create);

        /// <summary>Registers Endato client.</summary>
        public IHttpClientBuilder AddEndatoClient(Action<EndatoClientOptions> configure) => services.AddLyoHttpClient<EndatoClient, EndatoClientOptions>(configure, Create);

        /// <summary>Registers Endato client.</summary>
        public IHttpClientBuilder AddEndatoClient(EndatoClientOptions options) => services.AddLyoHttpClient<EndatoClient, EndatoClientOptions>(options, Create);
    }

    private static EndatoClient Create(IServiceProvider provider, HttpClient httpClient, EndatoClientOptions options)
        => new(options, provider.GetService<ILoggerFactory>(), httpClient);
}
