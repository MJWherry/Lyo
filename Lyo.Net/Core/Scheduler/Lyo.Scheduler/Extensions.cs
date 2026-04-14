using Lyo.Scheduler.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Scheduler;

/// <summary>Extension methods for registering the scheduler with dependency injection.</summary>
public static class SchedulerExtensions
{
    /// <summary>Adds the scheduler service to the service collection.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration for scheduler options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddScheduler(this IServiceCollection services, Action<SchedulerOptions>? configureOptions = null)
        => services.AddScheduler(new InMemorySchedulerStateStore(), configureOptions);

    /// <summary>Adds the scheduler service with a custom state store (e.g. cache-backed for persistence).</summary>
    public static IServiceCollection AddScheduler(this IServiceCollection services, ISchedulerStateStore stateStore, Action<SchedulerOptions>? configureOptions = null)
    {
        var options = new SchedulerOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton(stateStore);
        services.AddSingleton<ISchedulerService, SchedulerService>();
        return services;
    }

    /// <summary>Adds the scheduler service with a factory for the state store (e.g. for cache-backed that needs CacheService from DI).</summary>
    public static IServiceCollection AddScheduler(
        this IServiceCollection services,
        Func<IServiceProvider, ISchedulerStateStore> stateStoreFactory,
        Action<SchedulerOptions>? configureOptions = null)
    {
        var options = new SchedulerOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton(stateStoreFactory);
        services.AddSingleton<ISchedulerService, SchedulerService>();
        return services;
    }
}