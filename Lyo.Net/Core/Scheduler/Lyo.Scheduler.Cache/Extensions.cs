using Lyo.Cache;
using Lyo.Scheduler.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Scheduler.Cache;

/// <summary>Extension methods for registering the scheduler with cache-backed state persistence.</summary>
public static class SchedulerCacheExtensions
{
    /// <summary>Adds the scheduler service with cache-backed state store for persistence across restarts.</summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration for scheduler options</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>Requires ICacheService to be registered (e.g. via AddFusionCache or AddLocalCache).</remarks>
    public static IServiceCollection AddSchedulerWithCache(this IServiceCollection services, Action<SchedulerOptions>? configureOptions = null)
        => services.AddScheduler(sp => new CacheSchedulerStateStore(sp.GetRequiredService<ICacheService>()), configureOptions);
}