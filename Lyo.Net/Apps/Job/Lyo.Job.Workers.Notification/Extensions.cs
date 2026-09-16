using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Lyo.Exceptions;

namespace Lyo.Job.Workers.Notification;

/// <summary>
/// Registration for the notification workers. Each is a hosted service bound to its own worker type and queue, so a host can run one, two, or all three.
/// <para>
/// Prerequisites the host must already have registered: <c>IMqService</c>, <c>IJobClient</c>, and <c>IJobEventPublisher</c>, plus the channel service the worker sends
/// through — <c>ILyoHttpClient</c> for webhooks, <c>IEmailService</c> for email, <c>ISmsService</c> for SMS.
/// </para>
/// </summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds <see cref="WebhookNotificationWorker" /> with options configured inline.</summary>
        public IServiceCollection AddWebhookNotificationWorker(Action<WebhookNotificationWorkerOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new WebhookNotificationWorkerOptions();
            configure(options);
            return services.AddWebhookNotificationWorker(options);
        }

        /// <summary>Adds <see cref="WebhookNotificationWorker" /> with a prepared options instance. Validates the options at registration.</summary>
        public IServiceCollection AddWebhookNotificationWorker(WebhookNotificationWorkerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.AddHostedService<WebhookNotificationWorker>();
            return services;
        }

        /// <summary>Adds <see cref="WebhookNotificationWorker" /> and binds options from configuration. A missing section leaves the declared defaults in place.</summary>
        public IServiceCollection AddWebhookNotificationWorkerFromConfiguration(
            IConfiguration configuration,
            string sectionName = WebhookNotificationWorkerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new WebhookNotificationWorkerOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddWebhookNotificationWorker(options);
        }

        /// <summary>Adds <see cref="EmailNotificationWorker" /> with options configured inline.</summary>
        public IServiceCollection AddEmailNotificationWorker(Action<EmailNotificationWorkerOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new EmailNotificationWorkerOptions();
            configure(options);
            return services.AddEmailNotificationWorker(options);
        }

        /// <summary>Adds <see cref="EmailNotificationWorker" /> with a prepared options instance. Validates the options at registration.</summary>
        public IServiceCollection AddEmailNotificationWorker(EmailNotificationWorkerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.AddHostedService<EmailNotificationWorker>();
            return services;
        }

        /// <summary>Adds <see cref="EmailNotificationWorker" /> and binds options from configuration. A missing section leaves the declared defaults in place.</summary>
        public IServiceCollection AddEmailNotificationWorkerFromConfiguration(
            IConfiguration configuration,
            string sectionName = EmailNotificationWorkerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new EmailNotificationWorkerOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddEmailNotificationWorker(options);
        }

        /// <summary>Adds <see cref="SmsNotificationWorker" /> with options configured inline.</summary>
        public IServiceCollection AddSmsNotificationWorker(Action<SmsNotificationWorkerOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new SmsNotificationWorkerOptions();
            configure(options);
            return services.AddSmsNotificationWorker(options);
        }

        /// <summary>Adds <see cref="SmsNotificationWorker" /> with a prepared options instance. Validates the options at registration.</summary>
        public IServiceCollection AddSmsNotificationWorker(SmsNotificationWorkerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.TryAddSingleton(Options.Create(options));
            services.TryAddSingleton(options);
            services.AddHostedService<SmsNotificationWorker>();
            return services;
        }

        /// <summary>Adds <see cref="SmsNotificationWorker" /> and binds options from configuration. A missing section leaves the declared defaults in place.</summary>
        public IServiceCollection AddSmsNotificationWorkerFromConfiguration(IConfiguration configuration, string sectionName = SmsNotificationWorkerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new SmsNotificationWorkerOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddSmsNotificationWorker(options);
        }
    }
}
