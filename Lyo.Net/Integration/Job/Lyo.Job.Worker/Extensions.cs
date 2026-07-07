using Lyo.Api.Client;
using Lyo.Job.Models.Events;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
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
    /// <param name="maxRequeueCount">
    /// Max requeue count before DLQ routing. Null falls back to a registered <see cref="QueueWorkerOptions.DefaultMaxRequeueCount" />, or 5 when no options are registered, so a
    /// throwing worker can never retry forever by default.
    /// </param>
    /// <param name="dlqName">Dead-letter queue name. Null derives <c>job.run.{workerType}.dlq</c> so capped-out messages are preserved instead of dropped.</param>
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
            var workerOptions = sp.GetService<QueueWorkerOptions>();
            var effectiveMaxRequeue = maxRequeueCount ?? (workerOptions ?? new QueueWorkerOptions()).DefaultMaxRequeueCount;
            var effectiveDlqName = dlqName ?? $"{Models.Constants.Mq.QueueGetJobRunCreated(workerType)}.dlq";
            return (TWorker)Activator.CreateInstance(
                typeof(TWorker), mqService, apiClient, eventPublisher, workerType, apiBaseUrl, logger, metrics, effectiveMaxRequeue, effectiveDlqName)!;
        });

        services.AddHostedService(sp => sp.GetRequiredService<TWorker>());
        return services;
    }

    /// <summary>
    /// Registers a <typeparamref name="TWorker" /> like <see cref="AddJobWorker{TWorker}" />, additionally binding <see cref="QueueWorkerOptions" /> from configuration
    /// (section <see cref="QueueWorkerOptions.SectionName" />) so <c>DefaultMaxRequeueCount</c> is configurable per host.
    /// </summary>
    public static IServiceCollection AddJobWorkerFromConfiguration<TWorker>(
        this IServiceCollection services,
        IConfiguration configuration,
        string workerType,
        string apiBaseUrl,
        int? maxRequeueCount = null,
        string? dlqName = null,
        string configSectionName = QueueWorkerOptions.SectionName)
        where TWorker : JobWorkerBase
    {
        if (!services.Any(s => s.ServiceType == typeof(QueueWorkerOptions))) {
            services.AddSingleton<QueueWorkerOptions>(_ => {
                var options = new QueueWorkerOptions();
                var section = configuration.GetSection(configSectionName);
                if (section.Exists())
                    section.Bind(options);

                return options;
            });
        }

        return services.AddJobWorker<TWorker>(workerType, apiBaseUrl, maxRequeueCount, dlqName);
    }
}