using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Email;

public static class Extensions
{
    /// <param name="services">Service collection to add registrations to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the email service using a factory that returns options.</summary>
        /// <param name="configure">Callback that receives the provider and returns configured options</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddEmailService(Func<IServiceProvider, EmailServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<IValidateOptions<EmailServiceOptions>, EmailServiceOptionsValidator>();
            services.AddSingleton<EmailServiceOptions>(provider => {
                var options = configure(provider);
                var validator = new EmailServiceOptionsValidator();
                var validationResult = validator.Validate(null, options);
                OperationHelpers.ThrowIf(validationResult.Failed, $"EmailServiceOptions validation failed: {string.Join("; ", validationResult.Failures ?? [])}");
                return options;
            });

            services.AddSingleton<EmailService>(provider => {
                var options = provider.GetRequiredService<EmailServiceOptions>();
                var logger = provider.GetService<ILogger<EmailService>>();
                var metrics = provider.GetService<IMetrics>();
                return new(options, logger, metrics);
            });

            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<EmailService>());
            return services;
        }

        /// <summary>Registers the email service using a callback that mutates options.</summary>
        /// <param name="configure">Callback that receives the provider and the options object</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddEmailService(Action<IServiceProvider, EmailServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<IValidateOptions<EmailServiceOptions>, EmailServiceOptionsValidator>();
            services.AddSingleton<EmailServiceOptions>(provider => {
                var options = new EmailServiceOptions();
                configure(provider, options);
                var validator = new EmailServiceOptionsValidator();
                var validationResult = validator.Validate(null, options);
                OperationHelpers.ThrowIf(validationResult.Failed, $"EmailServiceOptions validation failed: {string.Join("; ", validationResult.Failures ?? [])}");
                return options;
            });

            services.AddSingleton<EmailService>(provider => {
                var options = provider.GetRequiredService<EmailServiceOptions>();
                var logger = provider.GetService<ILogger<EmailService>>();
                var metrics = provider.GetService<IMetrics>();
                return new(options, logger, metrics);
            });

            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<EmailService>());
            return services;
        }

        /// <summary>Registers the email service with an options callback.</summary>
        /// <param name="configure">Callback that receives the options object</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddEmailService(Action<EmailServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            services.AddSingleton<IValidateOptions<EmailServiceOptions>, EmailServiceOptionsValidator>();
            services.AddSingleton<EmailServiceOptions>(_ => {
                var options = new EmailServiceOptions();
                configure(options);
                var validator = new EmailServiceOptionsValidator();
                var validationResult = validator.Validate(null, options);
                OperationHelpers.ThrowIf(validationResult.Failed, $"EmailServiceOptions validation failed: {string.Join("; ", validationResult.Failures ?? [])}");
                return options;
            });

            services.AddSingleton<EmailService>(provider => {
                var options = provider.GetRequiredService<EmailServiceOptions>();
                var logger = provider.GetService<ILogger<EmailService>>();
                var metrics = provider.GetService<IMetrics>();
                return new(options, logger, metrics);
            });

            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<EmailService>());
            return services;
        }

        /// <summary>Registers the email service by binding options from configuration.</summary>
        /// <param name="configuration">Configuration root (for example context.Configuration).</param>
        /// <param name="configSectionName">Section to bind (defaults to "EmailServiceOptions").</param>
        /// <returns>The same collection so further calls can chain</returns>
        public IServiceCollection AddEmailServiceFromConfiguration(IConfiguration configuration, string configSectionName = EmailServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddSingleton<IValidateOptions<EmailServiceOptions>, EmailServiceOptionsValidator>();
            services.AddOptions<EmailServiceOptions>().Bind(configuration.GetSection(configSectionName)).ValidateOnStart();
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmailServiceOptions>>().Value);
            services.AddSingleton<EmailService>(provider => {
                var options = provider.GetRequiredService<IOptions<EmailServiceOptions>>().Value;
                var logger = provider.GetService<ILogger<EmailService>>();
                var metrics = provider.GetService<IMetrics>();
                return new(options, logger, metrics);
            });

            services.AddSingleton<IEmailService>(provider => provider.GetRequiredService<EmailService>());
            return services;
        }
    }
}