using Lyo.Cache;
using Lyo.Scheduler.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Scheduler.Cache;

/// <summary>DI helpers that register the scheduler with cache-backed state persistence.</summary>
public static class SchedulerCacheExtensions
{
    /// <summary>Registers the scheduler with a cache-backed state store so state survives restarts.</summary>
    /// <param name="services">Collection to add services to</param>
    /// <param name="configureOptions">Optional callback that fills scheduler options</param>
    /// <returns>The same collection, for chaining</returns>
    /// <remarks>Needs <c>ICacheService</c> already registered (via <c>AddFusionCache</c> or <c>AddLocalCache</c>, for example).</remarks>
    public static IServiceCollection AddSchedulerWithCache(this IServiceCollection services, Action<SchedulerOptions>? configureOptions = null)
        => services.AddScheduler(sp => new CacheSchedulerStateStore(sp.GetRequiredService<ICacheService>()), configureOptions);
}