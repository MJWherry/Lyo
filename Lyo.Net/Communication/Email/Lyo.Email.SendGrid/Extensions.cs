using Lyo.Email;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;

namespace Lyo.Email.SendGrid;

/// <summary>DI helpers that register the Twilio SendGrid email service.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers SendGrid email by binding <see cref="SendGridEmailOptions" /> from configuration.</summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="SendGridEmailOptions.SectionName" />.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddSendGridEmailServiceFromConfiguration(IConfiguration configuration, string configSectionName = SendGridEmailOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new SendGridEmailOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddSendGridEmailService(options);
        }

        /// <summary>Registers SendGrid email using a callback that fills options.</summary>
        /// <param name="configure">Callback that receives a new options instance.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddSendGridEmailService(Action<SendGridEmailOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SendGridEmailOptions();
            configure(options);
            return services.AddSendGridEmailService(options);
        }

        /// <summary>Registers SendGrid email with a prepared options instance. Calls <see cref="SendGridEmailOptions.Validate" />. One <see cref="SendGridClient" /> is shared.</summary>
        /// <param name="options">SendGrid options to use.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddSendGridEmailService(SendGridEmailOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.AddSingleton(options);
            services.AddSingleton(provider => new SendGridClient(provider.GetRequiredService<SendGridEmailOptions>().ApiKey));
            services.AddSingleton<ISendGridClient>(provider => provider.GetRequiredService<SendGridClient>());
            services.AddSingleton(provider => new SendGridEmailService(
                provider.GetRequiredService<SendGridEmailOptions>(), provider.GetRequiredService<ISendGridClient>(), provider.GetService<ILogger<SendGridEmailService>>(),
                provider.GetService<IMetrics>()));

            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<SendGridEmailService>());
            return services;
        }
    }
}
