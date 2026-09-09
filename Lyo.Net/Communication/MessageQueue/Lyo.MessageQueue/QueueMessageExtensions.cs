using System.Text.Json;
using Lyo.Common.Json;

namespace Lyo.MessageQueue;

/// <summary>Helpers that send queue messages wrapped in an envelope.</summary>
public static class QueueMessageExtensions
{
    /// <summary>Publishes to a queue inside a QueueMessageEnvelope. Use for queues consumed by QueueWorkerBase.</summary>
    /// <param name="mqService">Queue service.</param>
    /// <param name="queueName">Destination queue.</param>
    /// <param name="payload">Payload to wrap and send.</param>
    /// <param name="serializerOptions">Optional serializer options.</param>
    /// <param name="messageId">Optional message id. Defaults to a new GUID.</param>
    /// <param name="enqueuedAt">Optional enqueue time. Defaults to UtcNow.</param>
    /// <param name="traceId">Optional distributed-trace id.</param>
    /// <param name="priority">
    /// Optional priority (0 is default). Applied only when the transport implements <see cref="IPriorityMqService" /> and the queue supports priorities;
    /// otherwise the message is sent as usual.
    /// </param>
    /// <returns>True when the send succeeded.</returns>
    public static Task<bool> SendToQueueWithEnvelopeAsync<T>(
        this IMqService mqService,
        string queueName,
        T payload,
        JsonSerializerOptions? serializerOptions = null,
        string? messageId = null,
        DateTime? enqueuedAt = null,
        string? traceId = null,
        byte priority = 0)
    {
        var envelope = new QueueMessageEnvelope<T>(payload, 0, messageId ?? Guid.NewGuid().ToString("D"), enqueuedAt ?? DateTime.UtcNow, traceId);
        var options = serializerOptions ?? LyoJsonSerializerOptions.Create();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, options);
        return priority > 0 && mqService is IPriorityMqService priorityMqService
            ? priorityMqService.SendToQueueWithPriority(queueName, bytes, priority)
            : mqService.SendToQueue(queueName, bytes);
    }

    /// <summary>
    /// Subscribes with typed handling — the consume side of <see cref="SendToQueueWithEnvelopeAsync{T}" />. Deserialization uses the same
    /// autocorrect ladder as <see cref="QueueWorkerBase{TRequest, TResult}" /> (full envelope → payload-only → bare legacy <typeparamref name="T" />), so both enveloped and legacy
    /// producers work. Messages that cannot be deserialized on any path are acked (dropped) instead of being redelivered forever.
    /// </summary>
    /// <param name="mqService">Queue service.</param>
    /// <param name="queueName">Queue to subscribe to.</param>
    /// <param name="handler">Receives the deserialized payload and the envelope (null for legacy non-enveloped messages). Return true to requeue, false to ack.</param>
    /// <param name="serializerOptions">Optional serializer options.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the subscription was established.</returns>
    public static Task<bool> SubscribeToQueueAsync<T>(
        this IMqService mqService,
        string queueName,
        Func<T, QueueMessageEnvelope<T>?, Task<bool>> handler,
        JsonSerializerOptions? serializerOptions = null,
        CancellationToken ct = default)
    {
        var options = serializerOptions ?? LyoJsonSerializerOptions.Create();
        return mqService.SubscribeToQueue(
            queueName, messageBytes => {
                if (QueueWorkerHelpers.TryDeserializeMessage<T>(messageBytes, options, out var payload, out var envelope) && payload is not null)
                    return handler(payload, envelope);

                // Poison: do not return it to the broker — that would redeliver the same unparseable bytes forever.
                return Task.FromResult(false);
            }, ct);
    }
}