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
    /// <returns>True if the message was sent successfully.</returns>
    public static Task<bool> SendToQueueWithEnvelopeAsync<T>(
        this IMqService mqService,
        string queueName,
        T payload,
        JsonSerializerOptions? serializerOptions = null,
        string? messageId = null,
        DateTime? enqueuedAt = null,
        string? traceId = null)
    {
        var envelope = new QueueMessageEnvelope<T>(payload, 0, messageId ?? Guid.NewGuid().ToString("D"), enqueuedAt ?? DateTime.UtcNow, traceId);
        var options = serializerOptions ?? new JsonSerializerOptions();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, options);
        return mqService.SendToQueue(queueName, bytes);
    }
}