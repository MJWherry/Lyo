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

/// <summary>DI helpers that register the Twilio SMS service.</summary>
public static class Extensions
{
#if NET6_0_OR_GREATER
    private const string TwilioHttpClientName = "lyo-twilio-sms";
#endif
    /// <param name="services">Service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers Twilio SMS by binding options from configuration.</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration).</param>
        /// <param name="configSectionName">Section for TwilioOptions (defaults to "TwilioOptions").</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddTwilioSmsServiceFromConfiguration(IConfiguration configuration, string configSectionName = TwilioOptions.SectionName)
            => services.AddTwilioSmsService(configuration, configSectionName);

        /// <summary>Registers Twilio SMS from configuration (second overload).</summary>
        /// <param name="configuration">Configuration root (for example builder.Configuration).</param>
        /// <param name="configSectionName">Section for TwilioOptions (defaults to "TwilioOptions").</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddTwilioSmsService(IConfiguration configuration, string configSectionName = TwilioOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
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
}