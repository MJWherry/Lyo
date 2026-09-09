using Lyo.Http.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Typecast.Client;

/// <summary>DI helpers that register Typecast client with dependency injection.</summary>
public static class Extensions
{
    /// <summary>Registers Typecast client using configuration binding.</summary>
    public static IHttpClientBuilder AddTypecastClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = TypecastClientOptions.SectionName)
        => services.AddLyoHttpClient<TypecastClient, TypecastClientOptions>(configuration, configSectionName, Create);

    /// <summary>Registers Typecast client.</summary>
    public static IHttpClientBuilder AddTypecastClient(this IServiceCollection services, Action<TypecastClientOptions> configure)
        => services.AddLyoHttpClient<TypecastClient, TypecastClientOptions>(configure, Create);

    /// <summary>Registers Typecast client.</summary>
    public static IHttpClientBuilder AddTypecastClient(this IServiceCollection services, TypecastClientOptions options)
        => services.AddLyoHttpClient<TypecastClient, TypecastClientOptions>(options, Create);

    private static TypecastClient Create(IServiceProvider provider, HttpClient httpClient, TypecastClientOptions options)
        => new(options, provider.GetService<ILoggerFactory>(), httpClient);
}
