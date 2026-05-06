using System.Diagnostics;
using System.Text.Json;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Hosting;
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

    /// <summary>Wraps a raw byte payload in a new <see cref="QueueMessageEnvelope{T}" /> with requeue count 1.</summary>
    internal static byte[] WrapInEnvelope<TRequest>(TRequest payload, JsonSerializerOptions options)
    {
        var envelope = new QueueMessageEnvelope<TRequest>(payload, 1, Guid.NewGuid().ToString("N"), DateTime.UtcNow);
        return JsonSerializer.SerializeToUtf8Bytes(envelope, options);
    }
}

/// <summary>Abstract base class for queue workers. Implements <see cref="IHostedService" /> for automatic start/stop via the DI host.</summary>
/// <typeparam name="TRequest">The type of the deserialized request/message.</typeparam>
/// <typeparam name="TResult">A Result or BulkResult type - e.g. EmailResult, Result&lt;TRequest, TData&gt;, BulkResultFromRequest&lt;TRequest, TData&gt;.</typeparam>
public abstract class QueueWorkerBase<TRequest, TResult> : IHostedService, IDisposable, IHealth
    where TResult : ResultBase
{
    private readonly string? _dlqName;
    private readonly int? _maxRequeueCount;
    protected readonly ILogger Logger;
    protected readonly IMetrics Metrics;
    protected readonly IMqService MqService;
    protected readonly JsonSerializerOptions SerializerOptions;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    /// <summary>
    /// Number of messages currently being processed. Incremented at the start of each message handler and decremented when the handler completes. Used by
    /// <see cref="StopAsync" /> to wait until all in-flight work has finished before the host terminates.
    /// </summary>
    private int _inFlight;

    /// <summary>Current number of messages being concurrently processed by this worker. Exposed for health checks and monitoring.</summary>
    public int InFlightCount => _inFlight;

    protected string QueueName { get; }

    /// <summary>Gets a value indicating whether the worker is currently running.</summary>
    public bool IsRunning { get; private set; }
    
    /// <summary>Milliseconds to wait during <see cref="StopAsync" /> for in-flight messages to complete before giving up. Defaults to 30 000 ms (30 seconds).</summary>
    protected virtual int DrainTimeoutMs => 30_000;

    /// <summary>Initializes a new instance of the queue worker.</summary>
    /// <param name="mqService">The message queue service.</param>
    /// <param name="queueName">The queue to consume messages from.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics.</param>
    /// <param name="serializerOptions">Optional JSON serializer options.</param>
    /// <param name="maxRequeueCount">Maximum number of requeues before routing to the DLQ (or dropping). Null means no limit.</param>
    /// <param name="dlqName">
    /// Dead-letter queue name. When <paramref name="maxRequeueCount" /> is reached, the message is published here instead of being dropped. When null, messages that
    /// exceed the requeue limit are dropped (logged at Error level).
    /// </param>
    protected QueueWorkerBase(
        IMqService mqService,
        string queueName,
        ILogger? logger = null,
        IMetrics? metrics = null,
        JsonSerializerOptions? serializerOptions = null,
        int? maxRequeueCount = null,
        string? dlqName = null)
    {
        ArgumentHelpers.ThrowIfNull(mqService);
        ArgumentHelpers.ThrowIfNullOrEmpty(queueName);
        MqService = mqService;
        QueueName = queueName;
        Logger = logger ?? NullLogger.Instance;
        Metrics = metrics ?? NullMetrics.Instance;
        SerializerOptions = serializerOptions ?? new JsonSerializerOptions();
        _maxRequeueCount = maxRequeueCount;
        _dlqName = dlqName;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public virtual string HealthCheckName => $"queue-worker:{QueueName}";

    /// <inheritdoc />
    public virtual Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var metadata = new Dictionary<string, object?> { ["is_running"] = IsRunning, ["in_flight_count"] = _inFlight, ["queue_name"] = QueueName };
        var result = IsRunning
            ? HealthResult.Healthy(sw.Elapsed, $"Queue worker running ({_inFlight} in-flight)", metadata)
            : HealthResult.Unhealthy(sw.Elapsed, "Queue worker is not running", metadata);

        return Task.FromResult(result);
    }

    /// <summary>Starts the worker and begins processing messages from the queue. Called automatically by the host.</summary>
    public virtual async Task StartAsync(CancellationToken ct = default)
    {
        OperationHelpers.ThrowIfDisposed(_disposed, "QueueWorkerBase");
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
                    Interlocked.Increment(ref _inFlight);
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
                        if (shouldRequeue) {
                            // Legacy message: wrap in envelope so requeue count is tracked going forward
                            if (envelope == null) {
                                var wrappedBytes = QueueWorkerHelpers.WrapInEnvelope(payload, SerializerOptions);
                                await MqService.SendToQueue(QueueName, wrappedBytes).ConfigureAwait(false);
                                Metrics.IncrementCounter("queue.worker.messages.requeued", tags: [("queue", QueueName)]);
                                return false;
                            }

                            var maxRequeue = _maxRequeueCount;
                            if (maxRequeue.HasValue && envelope.RequeueCount >= maxRequeue.Value) {
                                await HandleMaxRequeueExceededAsync(envelope, messageBytes).ConfigureAwait(false);
                                return false;
                            }

                            var requeuedEnvelope = envelope with { RequeueCount = envelope.RequeueCount + 1 };
                            var requeueBytes = JsonSerializer.SerializeToUtf8Bytes(requeuedEnvelope, SerializerOptions);
                            await MqService.SendToQueue(QueueName, requeueBytes).ConfigureAwait(false);
                            Metrics.IncrementCounter("queue.worker.messages.requeued", tags: [("queue", QueueName)]);
                            return false;
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
                    finally {
                        Interlocked.Decrement(ref _inFlight);
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

    /// <summary>
    /// Stops the worker gracefully. Signals cancellation, then waits up to <see cref="DrainTimeoutMs" /> milliseconds for all in-flight messages to finish processing before
    /// returning. Called automatically by the host on shutdown.
    /// </summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!IsRunning) {
            Logger.LogWarning("Worker for queue {QueueName} is not running.", QueueName);
            return;
        }

        Logger.LogInformation("Stopping worker for queue {QueueName} ({InFlight} in-flight)...", QueueName, _inFlight);
        _cancellationTokenSource?.Cancel();

        // Drain: wait for in-flight handlers to complete.
        var deadline = DateTime.UtcNow.AddMilliseconds(DrainTimeoutMs);
        while (_inFlight > 0 && DateTime.UtcNow < deadline) {
            try {
                await Task.Delay(100, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
        }

        if (_inFlight > 0)
            Logger.LogWarning("Worker for queue {QueueName} stopped with {InFlight} in-flight message(s) still active (drain timeout reached)", QueueName, _inFlight);

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

    private async Task HandleMaxRequeueExceededAsync(QueueMessageEnvelope<TRequest> envelope, byte[] originalBytes)
    {
        Logger.LogError(
            "Message {MessageId} from queue {QueueName} exceeded max requeue count ({RequeueCount}). " + "Routing to DLQ: {DlqName}", envelope.MessageId, QueueName,
            envelope.RequeueCount, _dlqName ?? "(dropped)");

        Metrics.IncrementCounter("queue.worker.messages.dropped.max_requeue", tags: [("queue", QueueName)]);
        if (!_dlqName.IsNullOrWhitespace()) {
            try {
                await MqService.SendToQueue(_dlqName, originalBytes).ConfigureAwait(false);
                Metrics.IncrementCounter("queue.worker.messages.dlq", tags: [("queue", QueueName), ("dlq", _dlqName)]);
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Failed to send message {MessageId} to DLQ {DlqName}", envelope.MessageId, _dlqName);
            }
        }
    }
}