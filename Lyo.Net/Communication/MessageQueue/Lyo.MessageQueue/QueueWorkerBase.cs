using System.Diagnostics;
using System.Text.Json;
using Lyo.Common.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Extensions;
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
    /// <summary>Requeue when Metadata["requeue"] is true; skip requeue when it is false (including failures); otherwise use !isSuccess.</summary>
    internal static bool GetShouldRequeue(bool isSuccess, IReadOnlyDictionary<string, object>? metadata)
    {
        if (metadata?.TryGetValue("requeue", out var v) == true && v is bool b)
            return b;

        return !isSuccess;
    }

    /// <summary>
    /// Deserializes a queue message through an autocorrect ladder. Envelope-shaped JSON is tried first as a full <see cref="QueueMessageEnvelope{T}" />; on failure the
    /// <c>Payload</c> element is deserialized as <typeparamref name="TRequest" /> and envelope metadata is rebuilt from the root JSON. Non-envelope JSON is treated
    /// as a raw legacy <typeparamref name="TRequest" />. Returns false when no path recovers the message (caller should treat it as poison).
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
            var hasEnvelopeShape = root.ValueKind == JsonValueKind.Object && (root.TryGetProperty("RequeueCount", out var _) || root.TryGetProperty("requeueCount", out var _)) &&
                (root.TryGetProperty("Payload", out var _) || root.TryGetProperty("payload", out var _));

            if (hasEnvelopeShape) {
                // 1. Deserialize the full envelope.
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

                // 2. Autocorrect: deserialize Payload alone as TRequest, then rebuild envelope metadata from the root
                // so requeue tracking survives a partially malformed envelope. Never treat the whole envelope JSON as TRequest.
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

        // Older producers send bare TRequest JSON with no envelope.
        try {
            payload = JsonSerializer.Deserialize<TRequest>(messageBytes, options);
        }
        catch (JsonException) {
            return false;
        }

        return payload is not null;
    }

    /// <summary>Deserializes a payload element as <typeparamref name="TRequest" />, including double-encoded payloads (a JSON string that itself holds payload JSON).</summary>
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
        => TryGetPropertyIgnoreCase(root, pascalName, out var el) && TypeConversion.TryFromJsonElement<int>(el, out var i) ? i : null;

    private static string? GetString(JsonElement root, string pascalName)
        => TryGetPropertyIgnoreCase(root, pascalName, out var el) && TypeConversion.TryFromJsonElement<string>(el, out var s) ? s : null;

    private static DateTime? GetDateTime(JsonElement root, string pascalName)
        => TryGetPropertyIgnoreCase(root, pascalName, out var el) && TypeConversion.TryFromJsonElement<DateTime>(el, out var dt) ? dt : null;
}

/// <summary>Abstract worker that consumes a queue. Implements <see cref="IHostedService" /> so the DI host starts and stops it.</summary>
/// <typeparam name="TRequest">Deserialized request/message type.</typeparam>
/// <typeparam name="TResult">A Result or BulkResult type — for example EmailResult, Result&lt;TRequest, TData&gt;, BulkResultFromRequest&lt;TRequest, TData&gt;.</typeparam>
public abstract class QueueWorkerBase<TRequest, TResult> : IHostedService, IDisposable, IHealth
    where TResult : ResultBase
{
    protected readonly ILogger Logger;
    protected readonly IMetrics Metrics;
    protected readonly IMqService MqService;
    protected readonly JsonSerializerOptions SerializerOptions;
    private readonly string? _dlqName;
    private readonly int? _maxRequeueCount;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    /// <summary>
    /// How many messages are in flight. Raised when a handler starts and lowered when it finishes. <see cref="StopAsync" />
    /// waits for this to reach zero before the host exits.
    /// </summary>
    private int _inFlight;

    /// <summary>How many messages this worker is handling right now. Intended for health checks and monitoring.</summary>
    public int InFlightCount => _inFlight;

    protected string QueueName { get; }

    /// <summary>Whether the worker is running.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Base delay between retries, scaled linearly by attempt number. Applied only when the transport supports delayed delivery (<see cref="IDelayedMqService" />);
    /// otherwise retries republish immediately. Null or zero means no delay. Assigned by DI (for example <c>AddJobWorker</c> from <see cref="QueueWorkerOptions.RequeueDelay" />) rather than
    /// the constructor to keep binary compatibility; workers may also assign it themselves.
    /// </summary>
    public TimeSpan? RequeueDelay { get; set; }

    /// <summary>
    /// Drain budget for <see cref="StopAsync" />. Assigned by DI (for example <c>AddJobWorker</c> from <see cref="QueueWorkerOptions.DrainTimeout" />) rather
    /// than the constructor to keep binary compatibility. Null uses <see cref="QueueWorkerOptions.DefaultDrainTimeout" />. Increasing this without also increasing the host's
    /// <c>ShutdownTimeout</c> does nothing, because the host cancels the stop token first.
    /// </summary>
    public TimeSpan? DrainTimeout { get; set; }

    /// <summary>How long <see cref="StopAsync" /> waits for in-flight messages, in milliseconds, before giving up. Default is 30 000 ms (30 seconds).</summary>
    protected virtual int DrainTimeoutMs => (int)(DrainTimeout ?? QueueWorkerOptions.DefaultDrainTimeout).TotalMilliseconds;

    /// <summary>Envelope metadata for the message in flight, when the transport uses <see cref="QueueMessageEnvelope{T}" />.</summary>
    protected QueueMessageEnvelope<TRequest>? CurrentMessageEnvelope { get; private set; }

    /// <summary>Constructs the queue worker.</summary>
    /// <param name="mqService">Message-queue transport.</param>
    /// <param name="queueName">Queue this worker consumes.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics sink.</param>
    /// <param name="serializerOptions">Optional JSON serializer options.</param>
    /// <param name="maxRequeueCount">
    /// Max requeues before the DLQ (or drop). Null means unlimited. DI paths (for example <c>AddJobWorker</c>) resolve a
    /// default from <see cref="QueueWorkerOptions.DefaultMaxRequeueCount" /> before calling this constructor.
    /// </param>
    /// <param name="dlqName">
    /// Dead-letter queue. When <paramref name="maxRequeueCount" /> is reached, the message is published here instead of dropped. When null, messages that
    /// exceed the requeue limit are dropped (logged at Error).
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
        SerializerOptions = serializerOptions ?? LyoJsonSerializerOptions.Create();
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

    /// <summary>Starts the worker and begins consuming the queue. The host invokes this automatically.</summary>
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
    /// Stops the worker cleanly. Signals cancellation, then waits up to <see cref="DrainTimeoutMs" /> milliseconds for in-flight messages to finish before
    /// returning. The host invokes this on shutdown.
    /// </summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!IsRunning) {
            Logger.LogWarning("Worker for queue {QueueName} is not running.", QueueName);
            return;
        }

        Logger.LogInformation("Stopping worker for queue {QueueName} ({InFlight} in-flight)...", QueueName, _inFlight);
        _cancellationTokenSource?.Cancel();

        // Drain: wait until in-flight handlers finish.
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

    /// <summary>Handles one message.</summary>
    protected abstract Task<TResult> DoWorkAsync(TRequest request, CancellationToken ct);

    /// <summary>
    /// Subscription handler. Always returns false (ack) except during host shutdown — retries use counted application-level requeues
    /// (<see cref="ApplicationRequeueAsync" />) so a poison message or a <see cref="DoWorkAsync" /> that keeps throwing cannot loop forever on broker redelivery.
    /// </summary>
    private async Task<bool> ProcessMessageAsync(byte[] messageBytes)
    {
        Interlocked.Increment(ref _inFlight);
        using var timer = Metrics.StartTimer("queue.worker.message.processing.duration", [("queue", QueueName)]);
        Metrics.IncrementCounter("queue.worker.messages.received", tags: [("queue", QueueName)]);
        try {
            if (!QueueWorkerHelpers.TryDeserializeMessage<TRequest>(messageBytes, SerializerOptions, out var payload, out var envelope) || payload is null) {
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
                // Host shutdown: return the message to the broker unchanged so another consumer (or a restart) can take it. Not a retry.
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
            // Requeue/poison bookkeeping failed. Ack anyway — broker redelivery would skip the requeue count and loop forever.
            Logger.LogError(ex, "Error handling message from queue {QueueName}; message dropped to avoid infinite redelivery", QueueName);
            Metrics.RecordError("queue.worker.message.processing.error", ex, [("queue", QueueName)]);
            return false;
        }
        finally {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    /// <summary>
    /// Retries a failed message by republishing with an incremented <see cref="QueueMessageEnvelope{T}.RequeueCount" />. Legacy (non-enveloped) messages are wrapped so the
    /// count is tracked afterward. When the count hits the max requeue limit the message goes to the DLQ (or is dropped). Always returns false — the original delivery is acked
    /// and the republished copy is the retry.
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

        // Linear backoff (base delay times attempt) via broker delayed delivery when the transport supports it.
        // Transports without delay republish immediately — an in-process wait would occupy a prefetch slot for the whole delay.
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

    /// <summary>Handles a message no recovery path can deserialize: forwards the original bytes to the DLQ when configured, otherwise drops it. Never throws.</summary>
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

    /// <summary>Publishes a message wrapped in a QueueMessageEnvelope. Use this when the consumer is a QueueWorkerBase.</summary>
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