using Lyo.Api.Client;
using Lyo.Exceptions;
using Lyo.Formatter;
using Lyo.Job.Models.Events;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Scheduler;

/// <summary>DI extensions for Job Scheduler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds <see cref="JobScheduler" /> as a hosted service and registers <see cref="IJobScheduler" />. Requires <c>IApiClient</c>, <c>IFormatterService</c>, and
        /// <c>IJobEventPublisher</c> (register via <c>Lyo.Job.Client.AddMqJobEventPublisher*</c> on scheduler/worker hosts).
        /// </summary>
        public IServiceCollection AddJobScheduler(JobSchedulerOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(Options.Create(options));
            services.AddSingleton(p => p.GetRequiredService<IOptions<JobSchedulerOptions>>().Value);
            return services.AddJobSchedulerCore();
        }

        /// <summary>
        /// Adds <see cref="JobScheduler" /> as a hosted service, binding options from the <see cref="JobSchedulerOptions.SectionName" /> configuration section and validating
        /// them on host start. Requires <c>IApiClient</c>, <c>IFormatterService</c>, and <c>IJobEventPublisher</c> (register via
        /// <c>Lyo.Job.Client.AddMqJobEventPublisher*</c>).
        /// </summary>
        public IServiceCollection AddJobScheduler(string configSectionName = JobSchedulerOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddSingleton<IValidateOptions<JobSchedulerOptions>, JobSchedulerOptionsValidator>();
            services.AddOptions<JobSchedulerOptions>()
                .BindConfiguration(configSectionName)
                .ValidateOnStart();

            services.AddSingleton(p => p.GetRequiredService<IOptions<JobSchedulerOptions>>().Value);
            return services.AddJobSchedulerCore();
        }

        private IServiceCollection AddJobSchedulerCore()
        {
            services.AddSingleton(sp => new JobScheduler(
                sp.GetRequiredService<JobSchedulerOptions>(),
                sp.GetRequiredService<IApiClient>(),
                sp.GetRequiredService<IFormatterService>(),
                sp.GetRequiredService<IJobEventPublisher>(),
                sp.GetService<ILogger<JobScheduler>>(),
                sp.GetService<IMetrics>(),
                sp.GetService<IMqService>()));

            services.AddSingleton<IJobScheduler>(p => p.GetRequiredService<JobScheduler>());
            services.AddHostedService(p => p.GetRequiredService<JobScheduler>());
            return services;
        }

        /// <summary>Adds <see cref="JobWorkflowEngine" /> as a hosted service that advances workflow runs on job completion.</summary>
        public IServiceCollection AddJobWorkflowEngine(JobWorkflowEngineOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            options.Validate();
            services.AddSingleton(options);
            services.AddHostedService<JobWorkflowEngine>();
            return services;
        }

        /// <summary>Adds <see cref="JobWorkflowEngine" />, binding options from configuration.</summary>
        public IServiceCollection AddJobWorkflowEngine(string configSectionName = JobWorkflowEngineOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(configSectionName);
            services.AddOptions<JobWorkflowEngineOptions>()
                .BindConfiguration(configSectionName)
                .Validate(o => o.GetValidationErrors().Count == 0, $"Invalid {nameof(JobWorkflowEngineOptions)}.")
                .ValidateOnStart();

            services.AddSingleton(p => p.GetRequiredService<IOptions<JobWorkflowEngineOptions>>().Value);
            services.AddHostedService<JobWorkflowEngine>();
            return services;
        }
    }
}
