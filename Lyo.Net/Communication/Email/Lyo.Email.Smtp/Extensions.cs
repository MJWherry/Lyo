using Lyo.Email;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Email.Smtp;

/// <summary>DI helpers that register the MailKit SMTP email service.</summary>
public static class Extensions
{
    /// <param name="services">Service collection to add registrations to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers SMTP email by binding <see cref="SmtpEmailOptions" /> from configuration.</summary>
        /// <param name="configuration">Configuration root.</param>
        /// <param name="configSectionName">Section to bind. Defaults to <see cref="SmtpEmailOptions.SectionName" />.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddSmtpEmailServiceFromConfiguration(IConfiguration configuration, string configSectionName = SmtpEmailOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            var options = new SmtpEmailOptions();
            configuration.GetSection(configSectionName).Bind(options);
            return services.AddSmtpEmailService(options);
        }

        /// <summary>Registers SMTP email using a callback that fills options.</summary>
        /// <param name="configure">Callback that receives a new options instance.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddSmtpEmailService(Action<SmtpEmailOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SmtpEmailOptions();
            configure(options);
            return services.AddSmtpEmailService(options);
        }

        /// <summary>Registers SMTP email with a prepared options instance. Calls <see cref="SmtpEmailOptions.Validate" />.</summary>
        /// <param name="options">SMTP options to use.</param>
        /// <returns>The same collection so further calls can chain.</returns>
        public IServiceCollection AddSmtpEmailService(SmtpEmailOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.AddSingleton(options);
            services.AddSingleton(provider => new SmtpEmailService(
                provider.GetRequiredService<SmtpEmailOptions>(), provider.GetService<ILogger<SmtpEmailService>>(), provider.GetService<IMetrics>()));

            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<SmtpEmailService>());
            return services;
        }
    }
}
