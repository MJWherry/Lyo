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

    /// <summary>
    /// Deserializes a queue message with an autocorrect ladder. Envelope-shaped JSON is unwrapped via the full <see cref="QueueMessageEnvelope{T}" /> first; if that fails, the
    /// <c>Payload</c> element alone is deserialized as <typeparamref name="TRequest" /> and the envelope metadata is reconstructed from the root JSON. Non-envelope JSON is
    /// deserialized as a raw legacy <typeparamref name="TRequest" />. Returns false when the message cannot be recovered by any path (caller should treat it as poison).
    /// </summary>
    internal static bool TryDeserializeMessage<TRequest>(byte[] messageBytes, JsonSerializerOptions options, out TRequest? payload, out QueueMessageEnvelope<TRequest>? envelope)
    {
        payload = default;
        envelope = null;
        JsonDocument doc;
        try {
            doc = JsonDocument.Parse(messageBytes);
        }
        catch (JsonException) {
            return false;
        }

        using (doc) {
            var root = doc.RootElement;
            var hasEnvelopeShape = root.ValueKind == JsonValueKind.Object &&
                (root.TryGetProperty("RequeueCount", out var _) || root.TryGetProperty("requeueCount", out var _)) &&
                (root.TryGetProperty("Payload", out var _) || root.TryGetProperty("payload", out var _));

            if (hasEnvelopeShape) {
                // 1. Full envelope deserialize.
                try {
                    var full = JsonSerializer.Deserialize<QueueMessageEnvelope<TRequest>>(messageBytes, options);
                    if (full is not null && full.Payload is not null) {
                        payload = full.Payload;
                        envelope = full;
                        return true;
                    }
                }
                catch (JsonException) {
                    /* Fall through to payload-only autocorrect */
                }

                // 2. Autocorrect: deserialize only the Payload element as TRequest, then rebuild envelope metadata from the root
                // so requeue tracking survives partially malformed envelopes. Never deserialize the whole envelope JSON as TRequest.
                if (!TryGetPropertyIgnoreCase(root, "Payload", out var payloadElement))
                    return false;

                if (!TryDeserializeElement(payloadElement, options, out payload) || payload is null)
                    return false;

                envelope = new(
                    payload, GetInt(root, "RequeueCount") ?? 0, GetString(root, "MessageId"), GetDateTime(root, "EnqueuedAt"), GetString(root, "TraceId"),
                    GetInt(root, "Version") ?? 1);

                return true;
            }
        }

        // Legacy producers publish the bare TRequest JSON without an envelope.
        try {
            payload = JsonSerializer.Deserialize<TRequest>(messageBytes, options);
        }
        catch (JsonException) {
            return false;
        }

        return payload is not null;
    }

    /// <summary>Deserializes a payload element as <typeparamref name="TRequest" />, also handling double-encoded payloads (a JSON string containing the payload JSON).</summary>
    private static bool TryDeserializeElement<TRequest>(JsonElement element, JsonSerializerOptions options, out TRequest? payload)
    {
        payload = default;
        try {
            payload = element.Deserialize<TRequest>(options);
            return true;
        }
        catch (JsonException) {
            if (element.ValueKind != JsonValueKind.String)
                return false;
        }

        try {
            payload = JsonSerializer.Deserialize<TRequest>(element.GetString()!, options);
            return true;
        }
        catch (JsonException) {
            return false;
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string pascalName, out JsonElement value)
        => root.TryGetProperty(pascalName, out value) || root.TryGetProperty(char.ToLowerInvariant(pascalName[0]) + pascalName[1..], out value);

    private static int? GetInt(JsonElement root, string pascalName)
        => TryGetPropertyIgnoreCase(root, pascalName, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i) ? i : null;

    private static string? GetString(JsonElement root, string pascalName) => TryGetPropertyIgnoreCase(root, pascalName, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static DateTime? GetDateTime(JsonElement root, string pascalName)
        => TryGetPropertyIgnoreCase(root, pascalName, out var el) && el.ValueKind == JsonValueKind.String && el.TryGetDateTime(out var dt) ? dt : null;
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

    /// <summary>
    /// Base delay between retry attempts, scaled linearly by the attempt number. Only honored when the transport supports delayed delivery (<see cref="IDelayedMqService" />);
    /// otherwise retries republish immediately. Null or zero = no delay. Set by DI registration (e.g. <c>AddJobWorker</c> from <see cref="QueueWorkerOptions.RequeueDelay" />)
    /// rather than the constructor to preserve binary compatibility; workers may also set it directly.
    /// </summary>
    public TimeSpan? RequeueDelay { get; set; }

    /// <summary>Milliseconds to wait during <see cref="StopAsync" /> for in-flight messages to complete before giving up. Defaults to 30 000 ms (30 seconds).</summary>
    protected virtual int DrainTimeoutMs => 30_000;

    /// <summary>Initializes a new instance of the queue worker.</summary>
    /// <param name="mqService">The message queue service.</param>
    /// <param name="queueName">The queue to consume messages from.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics.</param>
    /// <param name="serializerOptions">Optional JSON serializer options.</param>
    /// <param name="maxRequeueCount">
    /// Maximum number of requeues before routing to the DLQ (or dropping). Null means no limit. DI registration paths (e.g. <c>AddJobWorker</c>) resolve a configurable default
    /// from <see cref="QueueWorkerOptions.DefaultMaxRequeueCount" /> before invoking this constructor.
    /// </param>
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
        var result = await MqService.SubscribeToQueue(QueueName, ProcessMessageAsync, _cancellationTokenSource.Token).ConfigureAwait(false);

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

    /// <summary>Envelope metadata for the message currently being processed, when the transport uses <see cref="QueueMessageEnvelope{T}" />.</summary>
    protected QueueMessageEnvelope<TRequest>? CurrentMessageEnvelope { get; private set; }

    /// <summary>Processes a message.</summary>
    protected abstract Task<TResult> DoWorkAsync(TRequest request, CancellationToken ct);

    /// <summary>
    /// Message handler for the queue subscription. Always returns false (ack) except during host shutdown — retries happen via counted application-level requeues
    /// (<see cref="ApplicationRequeueAsync" />) so a bad message or a repeatedly-throwing <see cref="DoWorkAsync" /> can never spin in an infinite broker redelivery loop.
    /// </summary>
    private async Task<bool> ProcessMessageAsync(byte[] messageBytes)
    {
        Interlocked.Increment(ref _inFlight);
        using var timer = Metrics.StartTimer("queue.worker.message.processing.duration", [("queue", QueueName)]);
        Metrics.IncrementCounter("queue.worker.messages.received", tags: [("queue", QueueName)]);
        QueueMessageEnvelope<TRequest>? envelope = null;
        try {
            if (!QueueWorkerHelpers.TryDeserializeMessage<TRequest>(messageBytes, SerializerOptions, out var payload, out envelope) || payload is null) {
                Metrics.IncrementCounter("queue.worker.messages.deserialization.failed", tags: [("queue", QueueName)]);
                await HandlePoisonMessageAsync(messageBytes, "deserialization failed after envelope autocorrect").ConfigureAwait(false);
                return false;
            }

            bool isSuccess;
            IReadOnlyDictionary<string, object>? metadata = null;
            CurrentMessageEnvelope = envelope;
            try {
                var workResult = await DoWorkAsync(payload, _cancellationTokenSource!.Token).ConfigureAwait(false);
                isSuccess = workResult.IsSuccess;
                metadata = workResult.Metadata;
            }
            catch (OperationCanceledException) when (_cancellationTokenSource?.IsCancellationRequested ?? true) {
                // Host shutdown: hand the message back to the broker unchanged so another consumer (or a restart) picks it up. Not a retry.
                Logger.LogInformation("Processing cancelled during shutdown for queue {QueueName}; message returned to broker", QueueName);
                return true;
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Unhandled exception processing message from queue {QueueName} — retrying via counted requeue", QueueName);
                Metrics.RecordError("queue.worker.message.processing.error", ex, [("queue", QueueName)]);
                isSuccess = false;
            }
            finally {
                CurrentMessageEnvelope = null;
            }

            if (QueueWorkerHelpers.GetShouldRequeue(isSuccess, metadata))
                return await ApplicationRequeueAsync(payload, envelope, messageBytes).ConfigureAwait(false);

            Metrics.IncrementCounter("queue.worker.messages.processed", tags: [("queue", QueueName)]);
            return false;
        }
        catch (Exception ex) {
            // Requeue/poison bookkeeping itself failed. Ack anyway — broker redelivery would bypass the requeue count and loop forever.
            Logger.LogError(ex, "Error handling message from queue {QueueName}; message dropped to avoid infinite redelivery", QueueName);
            Metrics.RecordError("queue.worker.message.processing.error", ex, [("queue", QueueName)]);
            return false;
        }
        finally {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    /// <summary>
    /// Retries a failed message by republishing it with an incremented <see cref="QueueMessageEnvelope{T}.RequeueCount" />. Legacy (non-enveloped) messages are wrapped so the
    /// count is tracked going forward. When the count reaches the max requeue limit the message is routed to the DLQ (or dropped). Always returns false — the original delivery
    /// is acked and the republished copy is the retry.
    /// </summary>
    private async Task<bool> ApplicationRequeueAsync(TRequest payload, QueueMessageEnvelope<TRequest>? envelope, byte[] originalBytes)
    {
        envelope ??= new(payload, 0, Guid.NewGuid().ToString("N"), DateTime.UtcNow);
        if (_maxRequeueCount.HasValue && envelope.RequeueCount >= _maxRequeueCount.Value) {
            await HandleMaxRequeueExceededAsync(envelope, originalBytes).ConfigureAwait(false);
            return false;
        }

        var requeuedEnvelope = envelope with { RequeueCount = envelope.RequeueCount + 1 };
        var requeueBytes = JsonSerializer.SerializeToUtf8Bytes(requeuedEnvelope, SerializerOptions);

        // Linear backoff (base delay x attempt number) via broker-side delayed delivery when the transport supports it.
        // Transports without delay support republish immediately — an in-process wait here would hold a prefetch slot for the whole delay.
        if (RequeueDelay is { } baseDelay && baseDelay > TimeSpan.Zero && MqService is IDelayedMqService delayedMqService) {
            var delay = TimeSpan.FromTicks(baseDelay.Ticks * requeuedEnvelope.RequeueCount);
            await delayedMqService.SendToQueueDelayed(QueueName, requeueBytes, delay).ConfigureAwait(false);
            Metrics.IncrementCounter("queue.worker.messages.requeued.delayed", tags: [("queue", QueueName)]);
        }
        else
            await MqService.SendToQueue(QueueName, requeueBytes).ConfigureAwait(false);

        Metrics.IncrementCounter("queue.worker.messages.requeued", tags: [("queue", QueueName)]);
        return false;
    }

    /// <summary>Handles a message that cannot be deserialized by any recovery path: forwards the original bytes to the DLQ when configured, otherwise drops it. Never throws.</summary>
    private async Task HandlePoisonMessageAsync(byte[] originalBytes, string reason)
    {
        Logger.LogError(
            "Poison message from queue {QueueName} ({Size} bytes) — {Reason}. Routing to DLQ: {DlqName}", QueueName, originalBytes.Length, reason, _dlqName ?? "(dropped)");

        Metrics.IncrementCounter("queue.worker.messages.poison", tags: [("queue", QueueName)]);
        if (_dlqName.IsNullOrWhitespace())
            return;

        try {
            await MqService.CreateQueue(_dlqName).ConfigureAwait(false);
            await MqService.SendToQueue(_dlqName, originalBytes).ConfigureAwait(false);
            Metrics.IncrementCounter("queue.worker.messages.dlq", tags: [("queue", QueueName), ("dlq", _dlqName)]);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to send poison message to DLQ {DlqName}", _dlqName);
        }
    }

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
                await MqService.CreateQueue(_dlqName).ConfigureAwait(false);
                await MqService.SendToQueue(_dlqName, originalBytes).ConfigureAwait(false);
                Metrics.IncrementCounter("queue.worker.messages.dlq", tags: [("queue", QueueName), ("dlq", _dlqName)]);
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Failed to send message {MessageId} to DLQ {DlqName}", envelope.MessageId, _dlqName);
            }
        }
    }
}