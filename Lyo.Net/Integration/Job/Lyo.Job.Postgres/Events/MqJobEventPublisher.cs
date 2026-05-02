using System.Text.Json;
using Lyo.Job.Models;
using Lyo.Job.Models.Events;
using Lyo.MessageQueue;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Postgres.Events;

/// <summary>
/// Default <see cref="IJobEventPublisher"/> implementation backed by <see cref="IMqService"/>.
/// Uses the standard job MQ topology (exchange <c>job.events</c>, routing keys, and per-worker-type
/// queues) defined in <see cref="Constants.Mq"/>.
/// <para>
/// Register via <c>services.AddMqJobEventPublisher()</c> after registering your <see cref="IMqService"/>
/// implementation (e.g. <c>services.AddRabbitMq(...)</c>).
/// </para>
/// </summary>
public sealed class MqJobEventPublisher : IJobEventPublisher
{
    private readonly IMqService _mqService;
    private readonly ILogger<MqJobEventPublisher> _logger;

    public MqJobEventPublisher(IMqService mqService, ILogger<MqJobEventPublisher> logger)
    {
        _mqService = mqService;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConnected() => _mqService.IsConnected();

    /// <inheritdoc />
    public async Task SetupAsync(CancellationToken ct = default)
    {
        await _mqService.ConnectAsync(ct).ConfigureAwait(false);

        // Completion queue — scheduler subscribes here to detect finished runs.
        await _mqService.CreateQueue(Constants.Mq.QueueJobRunFinish, true, false, false, null, ct).ConfigureAwait(false);

        // Definition-update queue — scheduler subscribes here to refresh its cache.
        var defQueue = Constants.Mq.JobDefinitionChangeKey;
        await _mqService.CreateQueue(defQueue, true, false, false, null, ct).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(defQueue, Constants.Mq.JobEventExchange, Constants.Mq.JobDefinitionChangeKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PublishRunCreatedAsync(Guid runId, string workerType, CancellationToken ct = default)
    {
        var queue = Constants.Mq.QueueGetJobRunCreated(workerType);
        var data = JsonSerializer.SerializeToUtf8Bytes(runId);
        _logger.LogDebug("Publishing run {RunId} created → queue {Queue}", runId, queue);
        await _mqService.SendToQueue(queue, data).ConfigureAwait(false);
        await _mqService.SendToExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobRunCreatedRoutingKey, data).ConfigureAwait(false);
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
    public Task SubscribeToDefinitionUpdatesAsync(string subscriberQueueName, Func<byte[], Task<bool>> handler, CancellationToken ct = default)
        => _mqService.SubscribeToQueue(subscriberQueueName, handler, ct);

    /// <inheritdoc />
    public Task SubscribeToRunCompletionsAsync(Func<byte[], Task<bool>> handler, CancellationToken ct = default)
        => _mqService.SubscribeToQueue(Constants.Mq.QueueJobRunFinish, handler, ct);

    /// <inheritdoc />
    public async Task SubscribeToRunCancellationsAsync(string workerType, Func<Guid, Task> handler, CancellationToken ct = default)
    {
        var queueName = $"job.run.{workerType}.cancel";
        await _mqService.CreateQueue(queueName, true, false, false, null, ct).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(queueName, Constants.Mq.JobEventExchange, Constants.Mq.JobRunCancelledRoutingKey, ct).ConfigureAwait(false);
        await _mqService.SubscribeToQueue(
                queueName, async body => {
                    try {
                        var runId = JsonSerializer.Deserialize<Guid>(body);
                        await handler(runId).ConfigureAwait(false);
                        return false;
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error processing cancellation message");
                        return true;
                    }
                }, ct)
            .ConfigureAwait(false);
    }
}
