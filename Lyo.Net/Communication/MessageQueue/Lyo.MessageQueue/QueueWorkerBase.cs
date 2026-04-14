using System.Text.Json;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.MessageQueue;

internal static class QueueWorkerHelpers
{
    /// <summary>Requeue if Metadata["requeue"] is true; don't requeue if false (even on failure); otherwise default to !isSuccess.</summary>
    internal static bool GetShouldRequeue(bool isSuccess, IReadOnlyDictionary<string, object>? metadata)
    {
        if (metadata?.TryGetValue("requeue", out var v) == true && v is bool b)
            return b;

        return !isSuccess;
    }

    /// <summary>Tries to deserialize as QueueMessageEnvelope first; falls back to raw TRequest for legacy messages.</summary>
    internal static (TRequest? Payload, QueueMessageEnvelope<TRequest>? Envelope) DeserializeMessage<TRequest>(byte[] messageBytes, JsonSerializerOptions options)
    {
        try {
            using var doc = JsonDocument.Parse(messageBytes);
            var root = doc.RootElement;
            var hasEnvelopeShape = (root.TryGetProperty("RequeueCount", out var _) || root.TryGetProperty("requeueCount", out var _)) &&
                (root.TryGetProperty("Payload", out var _) || root.TryGetProperty("payload", out var _));

            if (hasEnvelopeShape) {
                var envelope = JsonSerializer.Deserialize<QueueMessageEnvelope<TRequest>>(messageBytes, options);
                if (envelope != null && envelope.Payload != null)
                    return (envelope.Payload, envelope);
            }
        }
        catch (JsonException) {
            /* Fall through to raw deserialize */
        }

        var payload = JsonSerializer.Deserialize<TRequest>(messageBytes, options);
        return (payload, null);
    }
}

/// <summary>Abstract base class for queue workers. TResult extends ResultBase (e.g. EmailResult, Result&lt;TRequest, TData&gt;, BulkResultFromRequest).</summary>
/// <typeparam name="TRequest">The type of the deserialized request/message.</typeparam>
/// <typeparam name="TResult">A Result or BulkResult type - e.g. EmailResult, Result&lt;TRequest, TData&gt;, BulkResultFromRequest&lt;TRequest, TData&gt;.</typeparam>
public abstract class QueueWorkerBase<TRequest, TResult> : IDisposable
    where TResult : ResultBase
{
    private readonly int? _maxRequeueCount;
    protected readonly ILogger Logger;
    protected readonly IMetrics Metrics;
    protected readonly IMqService MqService;
    protected readonly JsonSerializerOptions SerializerOptions;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    protected string QueueName { get; }

    /// <summary>Gets a value indicating whether the worker is currently running.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Initializes a new instance of the queue worker.</summary>
    /// <param name="maxRequeueCount">Maximum number of requeues before dropping the message. Null means no limit.</param>
    protected QueueWorkerBase(
        IMqService mqService,
        string queueName,
        ILogger? logger = null,
        IMetrics? metrics = null,
        JsonSerializerOptions? serializerOptions = null,
        int? maxRequeueCount = null)
    {
        ArgumentHelpers.ThrowIfNull(mqService, nameof(mqService));
        ArgumentHelpers.ThrowIfNullOrEmpty(queueName, nameof(queueName));
        MqService = mqService;
        QueueName = queueName;
        Logger = logger ?? NullLogger.Instance;
        Metrics = metrics ?? NullMetrics.Instance;
        SerializerOptions = serializerOptions ?? new JsonSerializerOptions();
        _maxRequeueCount = maxRequeueCount;
    }

    /// <summary>Disposes the worker and stops processing.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _disposed = true;
    }

    /// <summary>Starts the worker and begins processing messages from the queue.</summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_disposed)
            throw new ObjectDisposedException("QueueWorkerBase");

        if (IsRunning) {
            Logger.LogWarning("Worker for queue {QueueName} is already running.", QueueName);
            return;
        }

        if (!MqService.IsConnected()) {
            Logger.LogInformation("Connecting to message queue service...");
            await MqService.ConnectAsync(ct).ConfigureAwait(false);
        }

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsRunning = true;
        Logger.LogInformation("Starting worker for queue {QueueName}...", QueueName);
        var result = await MqService.SubscribeToQueue(
                QueueName, async messageBytes => {
                    using var timer = Metrics.StartTimer("queue.worker.message.processing.duration", [("queue", QueueName)]);
                    Metrics.IncrementCounter("queue.worker.messages.received", tags: [("queue", QueueName)]);
                    try {
                        var (payload, envelope) = QueueWorkerHelpers.DeserializeMessage<TRequest>(messageBytes, SerializerOptions);
                        if (payload == null) {
                            Logger.LogError("Failed to deserialize message from queue {QueueName} - null result", QueueName);
                            Metrics.IncrementCounter("queue.worker.messages.deserialization.failed", tags: [("queue", QueueName)]);
                            return true;
                        }

                        var workResult = await DoWorkAsync(payload, _cancellationTokenSource!.Token).ConfigureAwait(false);
                        var shouldRequeue = QueueWorkerHelpers.GetShouldRequeue(workResult.IsSuccess, workResult.Metadata);
                        if (shouldRequeue && envelope != null) {
                            var maxRequeue = _maxRequeueCount;
                            if (maxRequeue.HasValue && envelope.RequeueCount >= maxRequeue.Value) {
                                Logger.LogWarning(
                                    "Message {MessageId} from queue {QueueName} dropped after {RequeueCount} requeues (max {MaxRequeue})", envelope.MessageId, QueueName,
                                    envelope.RequeueCount, maxRequeue.Value);

                                Metrics.IncrementCounter("queue.worker.messages.dropped.max_requeue", tags: [("queue", QueueName)]);
                                return false;
                            }

                            var requeuedEnvelope = envelope with { RequeueCount = envelope.RequeueCount + 1 };
                            var requeueBytes = JsonSerializer.SerializeToUtf8Bytes(requeuedEnvelope, SerializerOptions);
                            await MqService.SendToQueue(QueueName, requeueBytes).ConfigureAwait(false);
                            Metrics.IncrementCounter("queue.worker.messages.requeued", tags: [("queue", QueueName)]);
                            return false;
                        }

                        if (shouldRequeue && envelope == null) {
                            Metrics.IncrementCounter("queue.worker.messages.requeued", tags: [("queue", QueueName)]);
                            return true;
                        }

                        Metrics.IncrementCounter("queue.worker.messages.processed", tags: [("queue", QueueName)]);
                        return false;
                    }
                    catch (JsonException ex) {
                        Logger.LogError(ex, "Failed to deserialize message from queue {QueueName}", QueueName);
                        Metrics.RecordError("queue.worker.message.deserialization.error", ex, [("queue", QueueName)]);
                        Metrics.IncrementCounter("queue.worker.messages.deserialization.failed", tags: [("queue", QueueName)]);
                        return true;
                    }
                    catch (Exception ex) {
                        Logger.LogError(ex, "Error processing message from queue {QueueName}", QueueName);
                        Metrics.RecordError("queue.worker.message.processing.error", ex, [("queue", QueueName)]);
                        return true;
                    }
                }, _cancellationTokenSource.Token)
            .ConfigureAwait(false);

        if (result) {
            Logger.LogInformation("Worker for queue {QueueName} started successfully.", QueueName);
            Metrics.IncrementCounter("queue.worker.started", tags: [("queue", QueueName)]);
            Metrics.RecordGauge("queue.worker.running", 1, [("queue", QueueName)]);
        }
        else {
            Logger.LogError("Failed to start worker for queue {QueueName}.", QueueName);
            Metrics.IncrementCounter("queue.worker.start.failed", tags: [("queue", QueueName)]);
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>Stops the worker and stops processing messages.</summary>
    public void Stop()
    {
        if (!IsRunning) {
            Logger.LogWarning("Worker for queue {QueueName} is not running.", QueueName);
            return;
        }

        Logger.LogInformation("Stopping worker for queue {QueueName}...", QueueName);
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        IsRunning = false;
        Metrics.IncrementCounter("queue.worker.stopped", tags: [("queue", QueueName)]);
        Metrics.RecordGauge("queue.worker.running", 0, [("queue", QueueName)]);
        Logger.LogInformation("Worker for queue {QueueName} stopped.", QueueName);
    }

    /// <summary>Processes a message.</summary>
    protected abstract Task<TResult> DoWorkAsync(TRequest request, CancellationToken ct);

    /// <summary>Sends a message to the specified queue, wrapped in a QueueMessageEnvelope. Use when publishing to queues consumed by QueueWorkerBase.</summary>
    /// <param name="queueName">The queue to send to.</param>
    /// <param name="payload">The payload to wrap and send.</param>
    /// <param name="serializerOptions">Optional serializer options. Defaults to the worker's SerializerOptions.</param>
    /// <param name="messageId">Optional message ID. Defaults to a new GUID.</param>
    /// <param name="enqueuedAt">Optional enqueue timestamp. Defaults to UtcNow.</param>
    /// <param name="traceId">Optional trace ID for distributed tracing.</param>
    /// <returns>True if the message was sent successfully.</returns>
    protected Task<bool> SendToQueueWithEnvelopeAsync<T>(
        string queueName,
        T payload,
        JsonSerializerOptions? serializerOptions = null,
        string? messageId = null,
        DateTime? enqueuedAt = null,
        string? traceId = null)
    {
        var envelope = new QueueMessageEnvelope<T>(payload, 0, messageId ?? Guid.NewGuid().ToString("N"), enqueuedAt ?? DateTime.UtcNow, traceId);
        var options = serializerOptions ?? SerializerOptions;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, options);
        return MqService.SendToQueue(queueName, bytes);
    }
}