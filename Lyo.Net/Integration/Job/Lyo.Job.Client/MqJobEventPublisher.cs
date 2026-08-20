using System.Diagnostics;
using System.Text.Json;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.MessageQueue;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Client;

/// <summary>
/// <see cref="IJobEventPublisher" /> for scheduler/worker hosts, backed by <see cref="IMqService" /> and optional <see cref="IJobClient" /> for worker-type discovery. Does
/// not use EF or <c>Lyo.Job.Postgres</c> — API hosts with a job database should use the Postgres publisher instead.
/// <para>Register via <c>services.AddMqJobEventPublisher()</c> after registering <see cref="IMqService" /> (e.g. RabbitMQ).</para>
/// </summary>
public sealed class MqJobEventPublisher : IJobEventPublisher
{
    private readonly IJobClient? _jobClient;
    private readonly ILogger<MqJobEventPublisher> _logger;
    private readonly JobMqOptions _mqOptions;
    private readonly IMqService _mqService;

    public MqJobEventPublisher(IMqService mqService, ILogger<MqJobEventPublisher> logger, IOptions<JobMqOptions> mqOptions, IJobClient? jobClient = null)
    {
        _mqService = mqService;
        _logger = logger;
        _mqOptions = mqOptions.Value;
        _jobClient = jobClient;
    }

    /// <inheritdoc />
    public bool IsConnected() => _mqService.IsConnected();

    /// <inheritdoc />
    public async Task SetupAsync(CancellationToken ct = default)
    {
        await _mqService.ConnectAsync(ct).ConfigureAwait(false);
        await EnsureQueueCreatedAsync(Constants.Mq.QueueJobRunFinish, null, ct).ConfigureAwait(false);
        var defQueue = Constants.Mq.JobDefinitionChangeKey;
        await EnsureQueueCreatedAsync(defQueue, null, ct).ConfigureAwait(false);
        await EnsureBoundAsync(defQueue, Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, ct).ConfigureAwait(false);
        var workerTypes = await ResolveWorkerTypesAsync(ct).ConfigureAwait(false);
        await SetupWorkerQueuesAsync(workerTypes, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PublishRunCreatedAsync(Guid runId, string workerType, int priority = 0, CancellationToken ct = default)
    {
        var queue = Constants.Mq.QueueGetJobRunCreated(workerType);
        _logger.LogDebug("Publishing run {RunId} created → queue {Queue} (priority {Priority})", runId, queue, priority);
        var clampedPriority = (byte)(priority < 0 ? 0 : priority > byte.MaxValue ? byte.MaxValue : priority);
        await _mqService.SendToQueueWithEnvelopeAsync(queue, runId, traceId: Activity.Current?.TraceId.ToString(), priority: clampedPriority).ConfigureAwait(false);
        await _mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunCreatedRoutingKey, JsonSerializer.SerializeToUtf8Bytes(runId)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PublishRunStartedAsync(Guid runId, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing run {RunId} started", runId);
        await _mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunStartedRoutingKey, JsonSerializer.SerializeToUtf8Bytes(runId)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PublishRunFinishedAsync(Guid runId, CancellationToken ct = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(runId);
        _logger.LogDebug("Publishing run {RunId} finished", runId);
        await _mqService.SendToQueue(Constants.Mq.QueueJobRunFinish, data).ConfigureAwait(false);
        await _mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunFinishedRoutingKey, data).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PublishRunCancelledAsync(Guid runId, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing run {RunId} cancelled", runId);
        await _mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunCancelledRoutingKey, JsonSerializer.SerializeToUtf8Bytes(runId)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PublishDefinitionUpdatedAsync(Guid definitionId, CancellationToken ct = default)
    {
        _logger.LogDebug("Publishing definition {DefinitionId} updated", definitionId);
        await _mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, JsonSerializer.SerializeToUtf8Bytes(definitionId))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task PublishAlertAsync(Guid definitionId, Guid? runId, JobAlertType alertType, string message, CancellationToken ct = default)
    {
        var alert = new {
            DefinitionId = definitionId,
            RunId = runId,
            AlertType = alertType,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogDebug("Publishing alert {AlertType} for definition {DefinitionId} run {RunId}", alertType, definitionId, runId);
        return _mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobAlertRoutingKey, JsonSerializer.SerializeToUtf8Bytes(alert));
    }

    /// <inheritdoc />
    public Task SubscribeToDefinitionUpdatesAsync(string subscriberQueueName, Func<byte[], Task<bool>> handler, CancellationToken ct = default)
        => _mqService.SubscribeToQueue(subscriberQueueName, handler, ct);

    /// <inheritdoc />
    public Task SubscribeToRunCompletionsAsync(Func<byte[], Task<bool>> handler, CancellationToken ct = default)
        => _mqService.SubscribeToQueue(Constants.Mq.QueueJobRunFinish, handler, ct);

    /// <inheritdoc />
    public async Task SubscribeToRunCancellationsAsync(string workerType, Func<Guid, Task> handler, CancellationToken ct = default, string? instanceSuffix = null)
    {
        // Cancellations are broadcast through the exchange to a per-instance exclusive queue. A shared per-worker-type queue would be a
        // competing-consumer queue: with scaled-out workers only one instance would receive each cancel, and if it is not the instance
        // executing the run the cancellation is silently lost.
        var suffix = string.IsNullOrWhiteSpace(instanceSuffix) ? Guid.NewGuid().ToString("N") : instanceSuffix;
        var queueName = Constants.Mq.QueueGetJobRunCancelInstance(workerType, suffix);
        if (!await _mqService.CreateQueue(queueName, false, true, true, null, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Failed to declare per-instance cancellation queue '{queueName}'.");

        await EnsureBoundAsync(queueName, Constants.Mq.JobEventExchange, Constants.Mq.JobRunCancelledRoutingKey, ct).ConfigureAwait(false);
        await _mqService.SubscribeToQueueAsync<Guid>(
                queueName, async (runId, _) => {
                    try {
                        await handler(runId).ConfigureAwait(false);
                        return false;
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error processing cancellation message for run {RunId}", runId);
                        return true;
                    }
                }, ct: ct)
            .ConfigureAwait(false);
    }

    private async Task SetupWorkerQueuesAsync(IReadOnlyList<string> workerTypes, CancellationToken ct)
    {
        if (workerTypes.Count == 0) {
            _logger.LogWarning(
                "No worker types configured for job run queue provisioning. Add JobMqOptions:WorkerTypes or ensure the Job API has definitions, or declare queues manually.");

            return;
        }

        // Cancellation queues are not provisioned here: each worker instance declares its own exclusive queue in
        // SubscribeToRunCancellationsAsync (shared cancel queues would drop cancels for scaled-out workers).
        _logger.LogInformation("Provisioning job run queues for {Count} worker type(s)", workerTypes.Count);
        foreach (var workerType in workerTypes) {
            var runQueue = Constants.Mq.QueueGetJobRunCreated(workerType);
            await EnsureQueueCreatedAsync(runQueue, null, ct).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<string>> ResolveWorkerTypesAsync(CancellationToken ct)
    {
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var workerType in _mqOptions.WorkerTypes) {
            if (!string.IsNullOrWhiteSpace(workerType))
                types.Add(workerType.Trim());
        }

        if (_jobClient is null)
            return types.ToList();

        try {
            var fromApi = await _jobClient.Definitions.GetDistinctWorkerTypesAsync(ct).ConfigureAwait(false);
            foreach (var workerType in fromApi)
                types.Add(workerType);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to resolve worker types from Job API; using JobMqOptions:WorkerTypes only");
        }

        return types.ToList();
    }

    private async Task EnsureQueueCreatedAsync(string queueName, IReadOnlyDictionary<string, object>? arguments, CancellationToken ct)
    {
        IDictionary<string, object>? args = null;
        if (arguments is not null) {
            args = new Dictionary<string, object>();
            foreach (var kv in arguments)
                args[kv.Key] = kv.Value;
        }

        if (!await _mqService.CreateQueue(queueName, true, false, false, args, ct).ConfigureAwait(false)) {
            throw new InvalidOperationException(
                $"Failed to declare queue '{queueName}'. If it already exists with different arguments, delete it in RabbitMQ and restart the host.");
        }
    }

    private async Task EnsureBoundAsync(string queueName, string exchangeName, string routingKey, CancellationToken ct)
    {
        if (!await _mqService.BindQueueToExchange(queueName, exchangeName, routingKey, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Failed to bind queue '{queueName}' to exchange '{exchangeName}' with routing key '{routingKey}'.");
    }
}