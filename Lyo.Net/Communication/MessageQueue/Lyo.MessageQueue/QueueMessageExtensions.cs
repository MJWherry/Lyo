using System.Text.Json;

namespace Lyo.MessageQueue;

/// <summary>Extension methods for sending queue messages with envelope wrapping.</summary>
public static class QueueMessageExtensions
{
    /// <summary>Sends a message to the specified queue, wrapped in a QueueMessageEnvelope. Use when publishing to queues consumed by QueueWorkerBase.</summary>
    /// <param name="mqService">The message queue service.</param>
    /// <param name="queueName">The queue to send to.</param>
    /// <param name="payload">The payload to wrap and send.</param>
    /// <param name="serializerOptions">Optional serializer options.</param>
    /// <param name="messageId">Optional message ID. Defaults to a new GUID.</param>
    /// <param name="enqueuedAt">Optional enqueue timestamp. Defaults to UtcNow.</param>
    /// <param name="traceId">Optional trace ID for distributed tracing.</param>
    /// <param name="priority">
    /// Optional message priority (0 = default). Honored only when the transport implements <see cref="IPriorityMqService" /> and the queue supports priorities;
    /// otherwise the message is sent normally.
    /// </param>
    /// <returns>True if the message was sent successfully.</returns>
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
        var options = serializerOptions ?? new JsonSerializerOptions();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, options);
        return priority > 0 && mqService is IPriorityMqService priorityMqService
            ? priorityMqService.SendToQueueWithPriority(queueName, bytes, priority)
            : mqService.SendToQueue(queueName, bytes);
    }

    /// <summary>
    /// Subscribes to a queue with typed message handling — the consuming counterpart of <see cref="SendToQueueWithEnvelopeAsync{T}" />. Messages are deserialized with the same
    /// autocorrect ladder used by <see cref="QueueWorkerBase{TRequest, TResult}" /> (full envelope → payload-only → bare legacy <typeparamref name="T" />), so enveloped and legacy
    /// producers are both supported. Messages that cannot be deserialized by any path are acknowledged (dropped) instead of being redelivered forever.
    /// </summary>
    /// <param name="mqService">The message queue service.</param>
    /// <param name="queueName">The queue to subscribe to.</param>
    /// <param name="handler">Receives the deserialized payload and the envelope (null for legacy non-enveloped messages). Return true to requeue the message, false to acknowledge it.</param>
    /// <param name="serializerOptions">Optional serializer options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the subscription was established.</returns>
    public static Task<bool> SubscribeToQueueAsync<T>(
        this IMqService mqService,
        string queueName,
        Func<T, QueueMessageEnvelope<T>?, Task<bool>> handler,
        JsonSerializerOptions? serializerOptions = null,
        CancellationToken ct = default)
    {
        var options = serializerOptions ?? new JsonSerializerOptions();
        return mqService.SubscribeToQueue(
            queueName, messageBytes => {
                if (QueueWorkerHelpers.TryDeserializeMessage<T>(messageBytes, options, out var payload, out var envelope) && payload is not null)
                    return handler(payload, envelope);

                // Poison: never hand it back to the broker — that would redeliver the same unparseable bytes forever.
                return Task.FromResult(false);
            }, ct);
    }
}