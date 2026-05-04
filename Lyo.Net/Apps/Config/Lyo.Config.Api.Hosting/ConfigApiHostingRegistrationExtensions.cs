using Lyo.Config.Api.Client;
using Lyo.Config.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Config.Api.Hosting;

public static class ConfigApiHostingRegistrationExtensions
{
    /// <summary>Registers singleton <see cref="ConfigApiResolvedLedger" /> and <see cref="ConfigApiPollingHostedService" />.</summary>
    /// <remarks>Prerequisite: register <see cref="IConfigApiClient"/> via extension method <c>Lyo.Config.Api.Client.ConfigApiHttpClientRegistration.AddConfigApiClientFromConfiguration</c>.</remarks>
    public static IServiceCollection AddConfigApiPolling(this IServiceCollection services, IConfiguration configuration,
        string sectionName = ConfigApiPollingOptions.SectionName)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        services.TryAddSingleton<ConfigApiResolvedLedger>();

        services.AddOptions<ConfigApiPollingOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(
                static o => {
                    o.ThrowIfMisconfiguredWhenEnabled();
                    return true;
                });

        services.AddHostedService<ConfigApiPollingHostedService>();

        return services;
    }

    /// <summary>Registers <see cref="IOptionsMonitor{TOptions}" /> hydrated from one definition key in the polled <see cref="ResolvedConfigRecord" /> ledger.</summary>
    public static IServiceCollection AddConfigApiOptions<TOptions>(this IServiceCollection services,
        string definitionKey,
        ConfigApiMissingDefinitionKeyBehavior missingDefinitionKeyBehavior = ConfigApiMissingDefinitionKeyBehavior.Throw)
        where TOptions : class, new()
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (definitionKey == null)
            throw new ArgumentNullException(nameof(definitionKey));

        services.TryAddSingleton<ConfigApiResolvedLedger>();

        services.AddSingleton<IOptionsMonitor<TOptions>>(
            sp => new ConfigApiOptionsMonitor<TOptions>(
                sp.GetRequiredService<ConfigApiResolvedLedger>(),
                definitionKey,
                missingDefinitionKeyBehavior));

        return services;
    }
}
