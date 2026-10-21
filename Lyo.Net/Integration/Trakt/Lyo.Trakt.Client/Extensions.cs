using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Trakt.Client;

/// <summary>DI helpers that register <see cref="TraktClient" />.</summary>
public static class Extensions
{
    /// <param name="services">DI collection being extended.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the client from configuration section <see cref="TraktClientOptions.SectionName" />.</summary>
        public IHttpClientBuilder AddTraktClientFromConfiguration(IConfiguration configuration, string configSectionName = TraktClientOptions.SectionName)
            => services.AddLyoHttpClient<TraktClient, TraktClientOptions>(configuration, configSectionName, Create);

        /// <summary>Registers the client and applies <paramref name="configure" /> before validation.</summary>
        public IHttpClientBuilder AddTraktClient(Action<TraktClientOptions> configure) => services.AddLyoHttpClient<TraktClient, TraktClientOptions>(configure, Create);

        /// <summary>Registers the client from a ready options instance.</summary>
        public IHttpClientBuilder AddTraktClient(TraktClientOptions options) => services.AddLyoHttpClient<TraktClient, TraktClientOptions>(options, Create);
    }

    private static TraktClient Create(IServiceProvider provider, HttpClient httpClient, TraktClientOptions options)
        => new(options, provider.GetService<ILoggerFactory>(), httpClient);
}