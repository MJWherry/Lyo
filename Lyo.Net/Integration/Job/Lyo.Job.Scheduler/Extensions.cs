using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Scheduler;

/// <summary>DI extensions for Job Scheduler.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds <see cref="JobScheduler" /> as a hosted service and registers <see cref="IJobScheduler" />. Requires <c>IApiClient</c>, <c>IFormatterService</c>, and
        /// <c>IJobEventPublisher</c> to be registered (e.g. via <c>services.AddMqJobEventPublisher()</c>).
        /// </summary>
        public IServiceCollection AddJobScheduler(JobSchedulerOptions options)
        {
            services.AddSingleton(Options.Create(options));
            services.AddSingleton(p => p.GetRequiredService<IOptions<JobSchedulerOptions>>().Value);
            services.AddSingleton<JobScheduler>();
            services.AddSingleton<IJobScheduler>(p => p.GetRequiredService<JobScheduler>());
            services.AddHostedService(p => p.GetRequiredService<JobScheduler>());
            return services;
        }

        /// <summary>
        /// Adds <see cref="JobScheduler" /> as a hosted service, binding options from the <c>"JobScheduler"</c> configuration section. Requires <c>IApiClient</c>,
        /// <c>IFormatterService</c>, and <c>IJobEventPublisher</c> to be registered (e.g. via <c>services.AddMqJobEventPublisher()</c>).
        /// </summary>
        public IServiceCollection AddJobScheduler()
        {
            services.AddOptions<JobSchedulerOptions>().BindConfiguration("JobScheduler");
            services.AddSingleton<JobScheduler>();
            services.AddSingleton<IJobScheduler>(p => p.GetRequiredService<JobScheduler>());
            services.AddHostedService(p => p.GetRequiredService<JobScheduler>());
            return services;
        }
    }
}