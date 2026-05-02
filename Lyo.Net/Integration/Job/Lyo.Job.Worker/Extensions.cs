using Lyo.Api.Client;
using Lyo.Job.Models.Events;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Worker;

/// <summary>DI extensions for the job worker SDK.</summary>
public static class Extensions
{
    /// <summary>
    /// Registers a <typeparamref name="TWorker" /> as a singleton hosted service. Requires <see cref="IMqService" />, <see cref="IApiClient" />, and
    /// <see cref="IJobEventPublisher" /> to be registered.
    /// </summary>
    /// <typeparam name="TWorker">The concrete worker type (must extend <see cref="JobWorkerBase" />).</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="workerType">Worker type string — must match the <c>WorkerType</c> on job definitions.</param>
    /// <param name="apiBaseUrl">Base URL of the Job API.</param>
    /// <param name="maxRequeueCount">Max requeue count before DLQ routing. Null = unlimited.</param>
    /// <param name="dlqName">Dead-letter queue name. Null = drop on requeue limit.</param>
    public static IServiceCollection AddJobWorker<TWorker>(
        this IServiceCollection services,
        string workerType,
        string apiBaseUrl,
        int? maxRequeueCount = null,
        string? dlqName = null)
        where TWorker : JobWorkerBase
    {
        services.AddSingleton<TWorker>(sp => {
            var mqService = sp.GetRequiredService<IMqService>();
            var apiClient = sp.GetRequiredService<IApiClient>();
            var eventPublisher = sp.GetRequiredService<IJobEventPublisher>();
            var logger = sp.GetService<ILogger<TWorker>>();
            var metrics = sp.GetService<IMetrics>();
            return (TWorker)Activator.CreateInstance(typeof(TWorker), mqService, apiClient, eventPublisher, workerType, apiBaseUrl, logger, metrics, maxRequeueCount, dlqName)!;
        });

        services.AddHostedService(sp => sp.GetRequiredService<TWorker>());
        return services;
    }
}