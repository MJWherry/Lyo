using Lyo.Api.Client;
using Lyo.Configuration;
using Lyo.Exceptions;
using Lyo.Formatter;
using Lyo.Job.Client;
using Lyo.Job.Models;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Security;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Worker;

/// <summary>DI helpers for the job worker SDK.</summary>
public static class Extensions
{
    /// <summary>
    /// Adds a <typeparamref name="TWorker" /> as a singleton hosted service. Needs <see cref="IMqService" />, <see cref="IJobClient" /> (or <see cref="IApiClient" />
    /// plus <paramref name="apiBaseUrl" /> to build one), and <see cref="IJobEventPublisher" /> already registered.
    /// </summary>
    /// <typeparam name="TWorker">Concrete worker type (must extend <see cref="JobWorkerBase" />).</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="workerType">Worker type string. Must match <c>WorkerType</c> on job definitions.</param>
    /// <param name="apiBaseUrl">Job API base URL. Used to register <see cref="IJobClient" /> when it is not already registered.</param>
    /// <param name="maxRequeueCount">
    /// Max requeues before DLQ routing. Null uses a registered <see cref="QueueWorkerOptions.DefaultMaxRequeueCount" />, or 5 when no options
    /// are registered, so a throwing worker cannot retry forever by default.
    /// </param>
    /// <param name="dlqName">Dead-letter queue name. Null becomes <c>job.run.{workerType}.dlq</c> so capped-out messages are kept instead of dropped.</param>
    public static IServiceCollection AddJobWorker<TWorker>(
        this IServiceCollection services,
        string workerType,
        string apiBaseUrl,
        int? maxRequeueCount = null,
        string? dlqName = null)
        where TWorker : JobWorkerBase
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(workerType);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(apiBaseUrl);
        EnsureJobClientRegistered(services, apiBaseUrl);
        if (services.All(s => s.ServiceType != typeof(IFormatterService)))
            services.AddFormatterService();

        services.TryAddSingleton<QueueWorkerOptions>();

        // StopAsync drains in-flight runs, but the generic host ShutdownTimeout caps that drain. The host
        // default is shorter than a full drain, which cut running jobs mid-execution and sent them down the shutdown hand-back path.
        // Raise it (never lower it) so the drain has room, plus extra time for deregistration.
        services.AddOptions<HostOptions>()
            .Configure<QueueWorkerOptions>((hostOptions, workerOptions) => {
                var required = workerOptions.DrainTimeout + workerOptions.ShutdownTimeoutHeadroom;
                if (hostOptions.ShutdownTimeout < required)
                    hostOptions.ShutdownTimeout = required;
            });

        services.AddSingleton<TWorker>(sp => {
            var mqService = sp.GetRequiredService<IMqService>();
            var jobClient = sp.GetRequiredService<IJobClient>();
            var eventPublisher = sp.GetRequiredService<IJobEventPublisher>();
            var parameterEncryption = sp.GetService<IJobParameterEncryptionService>();
            var logger = sp.GetService<ILogger<TWorker>>();
            var metrics = sp.GetService<IMetrics>();
            var workerOptions = sp.GetService<QueueWorkerOptions>() ?? new QueueWorkerOptions();
            var effectiveMaxRequeue = maxRequeueCount ?? workerOptions.DefaultMaxRequeueCount;
            var effectiveDlqName = dlqName ?? $"{Constants.Mq.QueueGetJobRunCreated(workerType)}.dlq";
            var worker = (TWorker)Activator.CreateInstance(
                typeof(TWorker), mqService, jobClient, eventPublisher, workerType, logger, metrics, effectiveMaxRequeue, effectiveDlqName, parameterEncryption)!;

            // Set after construction (not via ctor) so the QueueWorkerBase constructor signature stays binary-compatible.
            worker.RequeueDelay = workerOptions.RequeueDelay;
            worker.DrainTimeout = workerOptions.DrainTimeout;
            worker.Formatter = sp.GetService<IFormatterService>();
            return worker;
        });

        services.AddHostedService(sp => sp.GetRequiredService<TWorker>());
        return services;
    }

    /// <summary>
    /// Adds a <typeparamref name="TWorker" /> like <see cref="AddJobWorker{TWorker}" />, and also binds <see cref="QueueWorkerOptions" /> from configuration (section
    /// <see cref="QueueWorkerOptions.SectionName" />) so each host can set <c>DefaultMaxRequeueCount</c>.
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
                var options = LyoOptions.Bind<QueueWorkerOptions>(configuration, configSectionName);
                options.Validate();
                return options;
            });
        }

        return services.AddJobWorker<TWorker>(workerType, apiBaseUrl, maxRequeueCount, dlqName);
    }

    private static void EnsureJobClientRegistered(IServiceCollection services, string apiBaseUrl)
    {
        if (services.Any(s => s.ServiceType == typeof(IJobClient)))
            return;

        var routePrefix = apiBaseUrl.TrimEnd('/');
        services.AddJobClient(sp => sp.GetRequiredService<IApiClient>(), new() { RoutePrefix = routePrefix });
    }
}