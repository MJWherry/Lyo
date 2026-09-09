using Lyo.Scheduler.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Scheduler;

/// <summary>Helpers for registering the scheduler with dependency injection.</summary>
public static class SchedulerExtensions
{
    /// <param name="services">Collection to add services to</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the scheduler service on the collection.</summary>
        /// <param name="configureOptions">Optional callback that fills scheduler options</param>
        /// <returns>The same collection, for chaining</returns>
        public IServiceCollection AddScheduler(Action<SchedulerOptions>? configureOptions = null) => services.AddScheduler(new InMemorySchedulerStateStore(), configureOptions);

        /// <summary>Registers the scheduler with a custom state store (for example cache-backed persistence).</summary>
        public IServiceCollection AddScheduler(ISchedulerStateStore stateStore, Action<SchedulerOptions>? configureOptions = null)
        {
            var options = new SchedulerOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton(stateStore);
            services.AddSingleton<ISchedulerService, SchedulerService>();
            return services;
        }

        /// <summary>Registers the scheduler with a state-store factory (for example cache-backed stores that need CacheService from DI).</summary>
        public IServiceCollection AddScheduler(Func<IServiceProvider, ISchedulerStateStore> stateStoreFactory, Action<SchedulerOptions>? configureOptions = null)
        {
            var options = new SchedulerOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton(stateStoreFactory);
            services.AddSingleton<ISchedulerService, SchedulerService>();
            return services;
        }
    }
}