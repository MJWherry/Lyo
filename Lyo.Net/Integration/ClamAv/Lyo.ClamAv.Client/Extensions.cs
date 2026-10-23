using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lyo.ClamAv.Client;

/// <summary>DI helpers that register <see cref="ClamAvClient" />.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the client from configuration section <see cref="ClamAvClientOptions.SectionName" />.</summary>
        public IHttpClientBuilder AddClamAvClientFromConfiguration(IConfiguration configuration, string configSectionName = ClamAvClientOptions.SectionName)
            => Register(services, services.AddLyoHttpClient<ClamAvClient, ClamAvClientOptions>(configuration, configSectionName, Create));

        /// <summary>Registers the client and applies <paramref name="configure" /> before validation.</summary>
        public IHttpClientBuilder AddClamAvClient(Action<ClamAvClientOptions> configure)
            => Register(services, services.AddLyoHttpClient<ClamAvClient, ClamAvClientOptions>(configure, Create));

        /// <summary>Registers the client from a ready options instance.</summary>
        public IHttpClientBuilder AddClamAvClient(ClamAvClientOptions options)
            => Register(services, services.AddLyoHttpClient<ClamAvClient, ClamAvClientOptions>(options, Create));
    }

    private static ClamAvClient Create(IServiceProvider provider, HttpClient httpClient, ClamAvClientOptions options)
        => new(options, provider.GetService<ILogger<ClamAvClient>>(), httpClient);

    private static IHttpClientBuilder Register(IServiceCollection services, IHttpClientBuilder builder)
    {
        services.TryAddTransient<IClamAvClient>(sp => sp.GetRequiredService<ClamAvClient>());
        return builder;
    }
}
