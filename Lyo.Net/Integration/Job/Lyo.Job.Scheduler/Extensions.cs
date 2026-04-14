using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Scheduler;

/// <summary>DI extensions for Job Scheduler.</summary>
public static class Extensions
{
    /// <summary>Adds JobScheduler and configures options. Requires IApiClient, IFormatterService, and IRabbitMqService to be registered.</summary>
    public static IServiceCollection AddJobScheduler(this IServiceCollection services, JobSchedulerOptions options)
    {
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(p => p.GetRequiredService<IOptions<JobSchedulerOptions>>().Value);
        services.AddSingleton<JobScheduler>();
        return services;
    }

    /// <summary>Adds JobScheduler and configures options from configuration section "JobScheduler".</summary>
    public static IServiceCollection AddJobScheduler(this IServiceCollection services)
    {
        services.AddOptions<JobSchedulerOptions>().BindConfiguration("JobScheduler");
        services.AddSingleton<JobScheduler>();
        return services;
    }
}