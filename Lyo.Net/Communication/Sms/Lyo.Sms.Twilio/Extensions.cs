using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio.Clients;
#if NET6_0_OR_GREATER
using Twilio.Http;
#endif

namespace Lyo.Sms.Twilio;

/// <summary>Extension methods for registering Twilio SMS service with dependency injection.</summary>
public static class Extensions
{
    private const string TwilioHttpClientName = "lyo-twilio-sms";

    /// <summary>Adds Twilio SMS service using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
    /// <param name="configSectionName">The config section for TwilioOptions (default: "TwilioOptions").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTwilioSmsServiceFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = TwilioOptions.SectionName)
        => services.AddTwilioSmsService(configuration, configSectionName);

    /// <summary>Adds Twilio SMS service using configuration binding.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration (e.g. builder.Configuration).</param>
    /// <param name="configSectionName">The config section for TwilioOptions (default: "TwilioOptions").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTwilioSmsService(this IServiceCollection services, IConfiguration configuration, string configSectionName = TwilioOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
#if NET6_0_OR_GREATER
        services.AddHttpClient(TwilioHttpClientName);
#endif
        services.AddSingleton<IValidateOptions<TwilioOptions>, TwilioOptionsValidator>();
        services.AddOptions<TwilioOptions>().Bind(configuration.GetSection(configSectionName)).ValidateOnStart();
        services.AddSingleton<TwilioOptions>(sp => sp.GetRequiredService<IOptions<TwilioOptions>>().Value);
        services.AddSingleton<TwilioSmsService>(sp => {
            var options = sp.GetRequiredService<TwilioOptions>();
            var logger = sp.GetService<ILogger<TwilioSmsService>>();
            var metrics = sp.GetService<IMetrics>();
#if NET6_0_OR_GREATER
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(TwilioHttpClientName);
            var twilioHttpClient = new SystemNetHttpClient(httpClient);
            var restClient = new TwilioRestClient(options.AccountSid, options.AuthToken, options.AccountSid, null, twilioHttpClient);
#else
            var restClient = new TwilioRestClient(options.AccountSid, options.AuthToken);
#endif
            return new(options, restClient, logger, metrics);
        });

        services.AddSingleton<ISmsService>(sp => sp.GetRequiredService<TwilioSmsService>());
        services.AddSingleton<ISmsService<TwilioSmsResult>>(sp => sp.GetRequiredService<TwilioSmsService>());
        return services;
    }
}