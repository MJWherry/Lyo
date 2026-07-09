using System.Diagnostics;
using System.Text.Json;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Postgres.Database;
using Lyo.MessageQueue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Postgres.Events;

/// <summary>
/// Default <see cref="IJobEventPublisher" /> implementation backed by <see cref="IMqService" />. Uses the standard job MQ topology (exchange <c>job.events</c>, routing keys,
/// and per-worker-type queues) defined in <see cref="Constants.Mq" />.
/// <para>Register via <c>services.AddMqJobEventPublisher()</c> after registering your <see cref="IMqService" /> implementation (e.g. <c>services.AddRabbitMq(...)</c>).</para>
/// </summary>
public sealed class MqJobEventPublisher : IJobEventPublisher
{
    private readonly ILogger<MqJobEventPublisher> _logger;
    private readonly IMqService _mqService;
    private readonly JobMqOptions _mqOptions;
    private readonly IServiceScopeFactory? _scopeFactory;

    public MqJobEventPublisher(
        IMqService mqService,
        ILogger<MqJobEventPublisher> logger,
        IOptions<JobMqOptions> mqOptions,
        IServiceScopeFactory? scopeFactory = null)
    {
        _mqService = mqService;
        _logger = logger;
        _mqOptions = mqOptions.Value;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public bool IsConnected() => _mqService.IsConnected();

    /// <inheritdoc />
    public async Task SetupAsync(CancellationToken ct = default)
    {
        await _mqService.ConnectAsync(ct).ConfigureAwait(false);

        // Completion queue — scheduler subscribes here to detect finished runs.
        await EnsureQueueCreatedAsync(Constants.Mq.QueueJobRunFinish, null, ct).ConfigureAwait(false);

        // Definition-update queue — scheduler subscribes here to refresh its cache.
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
        var clampedPriority = (byte)Math.Clamp(priority, 0, byte.MaxValue);
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
        var alert = new { DefinitionId = definitionId, RunId = runId, AlertType = alertType, Message = message, Timestamp = DateTime.UtcNow };
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
    public Task SubscribeToRunCancellationsAsync(string workerType, Func<Guid, Task> handler, CancellationToken ct = default)
    {
        var queueName = Constants.Mq.QueueGetJobRunCancel(workerType);

        // Typed subscribe handles both enveloped and legacy raw-Guid messages, and acks unparseable messages instead of redelivering them forever.
        return _mqService.SubscribeToQueueAsync<Guid>(
                queueName, async (runId, _) => {
                    try {
                        await handler(runId).ConfigureAwait(false);
                        return false;
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error processing cancellation message for run {RunId}", runId);
                        return true;
                    }
                }, ct: ct);
    }

    private async Task SetupWorkerQueuesAsync(IReadOnlyList<string> workerTypes, CancellationToken ct)
    {
        if (workerTypes.Count == 0) {
            _logger.LogWarning(
                "No worker types configured for job run queue provisioning. Add JobMqOptions:WorkerTypes or create job definitions before startup, or declare queues manually.");
            return;
        }

        _logger.LogInformation("Provisioning job run queues for {Count} worker type(s)", workerTypes.Count);
        foreach (var workerType in workerTypes) {
            var runQueue = Constants.Mq.QueueGetJobRunCreated(workerType);
            await EnsureQueueCreatedAsync(runQueue, null, ct).ConfigureAwait(false);

            var cancelQueue = Constants.Mq.QueueGetJobRunCancel(workerType);
            await EnsureQueueCreatedAsync(cancelQueue, null, ct).ConfigureAwait(false);
            await EnsureBoundAsync(cancelQueue, Constants.Mq.JobEventExchange, Constants.Mq.JobRunCancelledRoutingKey, ct).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<string>> ResolveWorkerTypesAsync(CancellationToken ct)
    {
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var workerType in _mqOptions.WorkerTypes) {
            if (!string.IsNullOrWhiteSpace(workerType))
                types.Add(workerType.Trim());
        }

        if (_scopeFactory is null)
            return types.ToList();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<JobContext>();
        if (db is null)
            return types.ToList();

        var fromDb = await db.JobDefinitions.AsNoTracking().Select(d => d.WorkerType).Distinct().ToListAsync(ct).ConfigureAwait(false);
        foreach (var workerType in fromDb) {
            if (!string.IsNullOrWhiteSpace(workerType))
                types.Add(workerType.Trim());
        }

        return types.ToList();
    }

    private async Task EnsureQueueCreatedAsync(string queueName, IReadOnlyDictionary<string, object>? arguments, CancellationToken ct)
    {
        IDictionary<string, object>? args = arguments is null ? null : new Dictionary<string, object>(arguments);
        if (!await _mqService.CreateQueue(queueName, true, false, false, args, ct).ConfigureAwait(false))
            throw new InvalidOperationException(
                $"Failed to declare queue '{queueName}'. If it already exists with different arguments, delete it in RabbitMQ and restart the host.");
    }

    private async Task EnsureBoundAsync(string queueName, string exchangeName, string routingKey, CancellationToken ct)
    {
        if (!await _mqService.BindQueueToExchange(queueName, exchangeName, routingKey, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Failed to bind queue '{queueName}' to exchange '{exchangeName}' with routing key '{routingKey}'.");
    }
}
