using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
#if NETSTANDARD2_0
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.Email;

public static class Extensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds email service to the service collection.</summary>
        /// <param name="configure">Action that receives the service provider and returns the configured options</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEmailService(Func<IServiceProvider, EmailServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<IValidateOptions<EmailServiceOptions>, EmailServiceOptionsValidator>();
            services.AddSingleton<EmailServiceOptions>(provider => {
                var options = configure(provider);
                var validator = new EmailServiceOptionsValidator();
                var validationResult = validator.Validate(null, options);
                OperationHelpers.ThrowIf(validationResult.Failed, $"EmailServiceOptions validation failed: {string.Join("; ", validationResult?.Failures ?? [])}");
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

        /// <summary>Adds email service to the service collection.</summary>
        /// <param name="configure">Action that receives the service provider and config object to configure</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEmailService(Action<IServiceProvider, EmailServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<IValidateOptions<EmailServiceOptions>, EmailServiceOptionsValidator>();
            services.AddSingleton<EmailServiceOptions>(provider => {
                var options = new EmailServiceOptions();
                configure(provider, options);
                var validator = new EmailServiceOptionsValidator();
                var validationResult = validator.Validate(null, options);
                OperationHelpers.ThrowIf(validationResult.Failed, $"EmailServiceOptions validation failed: {string.Join("; ", validationResult?.Failures ?? [])}");
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

        /// <summary>Adds email service to the service collection.</summary>
        /// <param name="configure">Action that receives the config object to configure</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEmailService(Action<EmailServiceOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
            services.AddSingleton<IValidateOptions<EmailServiceOptions>, EmailServiceOptionsValidator>();
            services.AddSingleton<EmailServiceOptions>(_ => {
                var options = new EmailServiceOptions();
                configure(options);
                var validator = new EmailServiceOptionsValidator();
                var validationResult = validator.Validate(null, options);
                OperationHelpers.ThrowIf(validationResult.Failed, $"EmailServiceOptions validation failed: {string.Join("; ", validationResult?.Failures ?? [])}");
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

        /// <summary>Adds email service using configuration binding.</summary>
        /// <param name="configuration">The configuration (e.g. context.Configuration).</param>
        /// <param name="configSectionName">The configuration section name (defaults to "EmailServiceOptions").</param>
        /// <returns>The service collection for chaining</returns>
        public IServiceCollection AddEmailServiceFromConfiguration(IConfiguration configuration, string configSectionName = EmailServiceOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services, nameof(services));
            ArgumentHelpers.ThrowIfNull(configuration, nameof(configuration));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName, nameof(configSectionName));
            services.AddSingleton<IValidateOptions<EmailServiceOptions>, EmailServiceOptionsValidator>();
            services.AddOptions<EmailServiceOptions>().Bind(configuration.GetSection(configSectionName)).ValidateOnStart();
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