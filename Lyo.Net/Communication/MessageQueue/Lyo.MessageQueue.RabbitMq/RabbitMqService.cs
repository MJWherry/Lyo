using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Lyo.Common;
using Lyo.Common.Extensions;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Lyo.MessageQueue.RabbitMq;

/// <summary>RabbitMQ implementation of the message queue service interface. Provides robust error handling, metrics, logging, and connection management.</summary>
public sealed class RabbitMqService : IRabbitMqService, IAsyncDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private readonly Dictionary<string, (AsyncEventingBasicConsumer Consumer, string ConsumerTag, IChannel Channel)> _consumers = new();

    /// <summary>Delay wait queues (see <see cref="SendToQueueDelayed" />) already declared on the broker, to skip redundant declares on subsequent publishes.</summary>
    private readonly ConcurrentDictionary<string, byte> _declaredWaitQueues = new();

    private readonly HttpClient _httpClient;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly IMetrics _metrics;
    private readonly RabbitMqOptions _options;
    private readonly bool _ownsHttpClient;
    private readonly Dictionary<string, SemaphoreSlim> _processingSemaphores = new();
    private readonly JsonSerializerOptions _serializerOptions;

    private IConnection? _connection;
    private bool _disposed;
    private IChannel? _publishChannel; // Shared channel for publishing and queue management operations

    public RabbitMqService(
        RabbitMqOptions options,
        IConnectionFactory connectionFactory,
        HttpClient? httpClient = null,
        ILogger<RabbitMqService>? logger = null,
        IMetrics? metrics = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(connectionFactory);
        _options = options;
        _connectionFactory = connectionFactory;
        _logger = logger ?? NullLogger<RabbitMqService>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _serializerOptions = serializerOptions ?? LyoJsonSerializerOptions.Create();
        _ownsHttpClient = httpClient == null;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress ??= new($"{_options.AdminUrl}/api/");
        _httpClient.DefaultRequestHeaders.Authorization ??= new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}")));
    }

    /// <summary>Disconnects from the broker and releases owned resources. After disposal the service cannot be reused.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try {
            await DisconnectAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Error disconnecting from RabbitMQ during dispose");
        }

        _disposed = true;
        if (_ownsHttpClient)
            _httpClient.Dispose();

        _connectionLock.Dispose();
    }

    /// <inheritdoc />
    public string HealthCheckName => "rabbitmq";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var connection = await _connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
            sw.Stop();
            var ok = connection.IsOpen;
            return ok
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["connectionOpen"] = true })
                : HealthResult.Unhealthy(sw.Elapsed, "Connection not open");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        OperationHelpers.ThrowIfDisposed(_disposed, nameof(RabbitMqService));
        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (IsConnected()) {
                _logger.LogDebug("Already connected to RabbitMQ");
                return;
            }

            // Bounded retry for startup races where the broker is not accepting connections yet.
            var attempts = Math.Max(0, _options.ConnectRetryCount) + 1;
            for (var attempt = 1; attempt <= attempts; attempt++) {
                try {
                    await ConnectCoreAsync(ct).ConfigureAwait(false);
                    break;
                }
                catch (Exception ex) when (attempt < attempts && ex is not OperationCanceledException) {
                    _logger.LogWarning(ex, "Connect attempt {Attempt}/{Attempts} to RabbitMQ failed; retrying in {Delay}", attempt, attempts, _options.ConnectRetryDelay);
                    await Task.Delay(_options.ConnectRetryDelay, ct).ConfigureAwait(false);
                }
            }

            // Initialize defined queues if specified
            if (_options.DefinedQueues?.Any() ?? false)
                await InitializeDefinedQueues(ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
        finally {
            _connectionLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_disposed)
            return;

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            await CancelAllConsumersAsync(ct).ConfigureAwait(false);
            DisposeProcessingSemaphores();
            if (_publishChannel != null) {
                try {
                    await _publishChannel.CloseAsync(ct).ConfigureAwait(false);
                    await _publishChannel.DisposeAsync().ConfigureAwait(false);
                    _logger.LogDebug("RabbitMQ publish channel closed");
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Error closing RabbitMQ publish channel");
                }
                finally {
                    _publishChannel = null;
                }
            }

            // Close connection
            if (_connection != null) {
                try {
                    if (_connection.IsOpen)
                        await _connection.CloseAsync(ct).ConfigureAwait(false);

                    await _connection.DisposeAsync().ConfigureAwait(false);
                    _logger.LogInformation("RabbitMQ connection closed");
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Error closing RabbitMQ connection");
                }
                finally {
                    _connection = null;
                }
            }

            _declaredWaitQueues.Clear();
            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.ConnectionClosed, 1);
        }
        finally {
            _connectionLock.Release();
        }
    }

    public bool IsConnected() => (_connection?.IsOpen ?? false) && (_publishChannel?.IsOpen ?? false);

    public async Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
    {
        if (exchangeName.IsNullOrWhitespace()) {
            _logger.LogWarning("Cannot create exchange: exchange name is null or empty");
            return false;
        }

        if (exchangeType.IsNullOrWhitespace())
            exchangeType = "direct";

        if (!IsConnected()) {
            _logger.LogWarning("Cannot create exchange {ExchangeName}: not connected", exchangeName);
            return false;
        }

        arguments ??= new Dictionary<string, object>(0);
        try {
            await _publishChannel!.ExchangeDeclareAsync(exchangeName, exchangeType, durable, autoDelete, arguments!, cancellationToken: ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Created exchange {ExchangeName} ({ExchangeType}, Durable: {Durable}, AutoDelete: {AutoDelete})", exchangeName, exchangeType, durable, autoDelete);

            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to create exchange {ExchangeName}", exchangeName);
            return false;
        }
    }

    public async Task<bool> DeleteExchange(string exchangeName, bool ifUnused = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(exchangeName)) {
            _logger.LogWarning("Cannot delete exchange: exchange name is null or empty");
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot delete exchange {ExchangeName}: not connected", exchangeName);
            return false;
        }

        try {
            await _publishChannel!.ExchangeDeleteAsync(exchangeName, ifUnused, cancellationToken: ct).ConfigureAwait(false);
            _logger.LogInformation("Deleted exchange {ExchangeName}", exchangeName);
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to delete exchange {ExchangeName}", exchangeName);
            return false;
        }
    }

    public async Task<bool> CreateQueue(
        string queueName,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot create queue: queue name is null or empty");
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot create queue {QueueName}: not connected", queueName);
            return false;
        }

        arguments ??= new Dictionary<string, object>(0);
        using var timer = _metrics.StartTimer(Constants.Metrics.QueueOperationDuration, [(Constants.Metrics.Tags.Operation, "create"), (Constants.Metrics.Tags.Queue, queueName)]);
        var sw = Stopwatch.StartNew();
        try {
            var result = await _publishChannel!.QueueDeclareAsync(queueName, durable, exclusive, autoDelete, arguments!, cancellationToken: ct).ConfigureAwait(false);

            // Initialize processing semaphore for this queue if a (per-queue or global) processing limit is set
            var limit = _options.GetProcessingLimit(queueName);
            if (limit > 0 && !_processingSemaphores.ContainsKey(queueName)) {
                _processingSemaphores[queueName] = new(limit, limit);
                _logger.LogDebug("Created processing semaphore for queue {QueueName} with limit {Limit}", queueName, limit);
            }

            sw.Stop();
            _logger.LogInformation("Created queue {QueueName} (Durable: {Durable}, Exclusive: {Exclusive}, AutoDelete: {AutoDelete})", queueName, durable, exclusive, autoDelete);
            if (!_options.EnableMetrics)
                return true;

            _metrics.IncrementCounter(Constants.Metrics.QueueCreated, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
            _metrics.RecordHistogram(
                Constants.Metrics.QueueOperationDurationMs, sw.ElapsedMilliseconds, [(Constants.Metrics.Tags.Operation, "create"), (Constants.Metrics.Tags.Queue, queueName)]);

            return true;
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "Failed to create queue {QueueName}", queueName);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.QueueOperationFailed, 1, [(Constants.Metrics.Tags.Operation, "create"), (Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordError(Constants.Metrics.QueueOperationDuration, ex, [(Constants.Metrics.Tags.Operation, "create"), (Constants.Metrics.Tags.Queue, queueName)]);
            }

            return false;
        }
    }

    public async Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot delete queue: queue name is null or empty");
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot delete queue {QueueName}: not connected", queueName);
            return false;
        }

        using var timer = _metrics.StartTimer(Constants.Metrics.QueueOperationDuration, [(Constants.Metrics.Tags.Operation, "delete"), (Constants.Metrics.Tags.Queue, queueName)]);
        var sw = Stopwatch.StartNew();
        try {
            // Cancel consumer if exists
            if (_consumers.TryGetValue(queueName, out var consumerInfo)) {
                try {
                    await consumerInfo.Channel.BasicCancelAsync(consumerInfo.ConsumerTag, cancellationToken: ct).ConfigureAwait(false);
                    try {
                        await consumerInfo.Channel.CloseAsync(ct).ConfigureAwait(false);
                        await consumerInfo.Channel.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) {
                        _logger.LogWarning(ex, "Error closing channel for queue {QueueName} during deletion", queueName);
                    }

                    _consumers.Remove(queueName);
                    _logger.LogDebug("Cancelled consumer for queue {QueueName} before deletion", queueName);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to cancel consumer for queue {QueueName} before deletion", queueName);
                }
            }

            // Delete the queue
            await _publishChannel!.QueueDeleteAsync(queueName, ifUnused, ifEmpty, cancellationToken: ct).ConfigureAwait(false);

            // Clean up processing semaphore if exists
            if (_processingSemaphores.TryGetValue(queueName, out var semaphore)) {
                semaphore.Dispose();
                _processingSemaphores.Remove(queueName);
            }

            sw.Stop();
            _logger.LogInformation("Deleted queue {QueueName}", queueName);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.QueueDeleted, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordHistogram(
                    Constants.Metrics.QueueOperationDurationMs, sw.ElapsedMilliseconds, [(Constants.Metrics.Tags.Operation, "delete"), (Constants.Metrics.Tags.Queue, queueName)]);
            }

            return true;
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "Failed to delete queue {QueueName}", queueName);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.QueueOperationFailed, 1, [(Constants.Metrics.Tags.Operation, "delete"), (Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordError(Constants.Metrics.QueueOperationDuration, ex, [(Constants.Metrics.Tags.Operation, "delete"), (Constants.Metrics.Tags.Queue, queueName)]);
            }

            return false;
        }
    }

    public async Task<bool> ClearQueue(string queueName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot clear queue: queue name is null or empty");
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot clear queue {QueueName}: not connected", queueName);
            return false;
        }

        using var timer = _metrics.StartTimer(Constants.Metrics.QueueOperationDuration, [(Constants.Metrics.Tags.Operation, "clear"), (Constants.Metrics.Tags.Queue, queueName)]);
        var sw = Stopwatch.StartNew();
        try {
            // Try Management API first
            var vhost = _options.VirtualHost == "/" ? "%2F" : Uri.EscapeDataString(_options.VirtualHost);
            var purgeResponse = await _httpClient.DeleteAsync($"queues/{vhost}/{Uri.EscapeDataString(queueName)}/contents", ct).ConfigureAwait(false);
            if (purgeResponse.IsSuccessStatusCode) {
                sw.Stop();
                _logger.LogInformation("Cleared queue {QueueName} using Management API", queueName);
                if (_options.EnableMetrics) {
                    _metrics.IncrementCounter(Constants.Metrics.QueueCleared, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                    _metrics.RecordHistogram(
                        Constants.Metrics.QueueOperationDurationMs, sw.ElapsedMilliseconds,
                        [(Constants.Metrics.Tags.Operation, "clear"), (Constants.Metrics.Tags.Queue, queueName)]);
                }

                return true;
            }

            // Fallback to channel purge
            _logger.LogWarning("Management API purge failed for queue {QueueName} (Status: {StatusCode}), using channel purge", queueName, purgeResponse.StatusCode);
            await _publishChannel!.QueuePurgeAsync(queueName, ct).ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("Cleared queue {QueueName} using channel purge", queueName);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.QueueCleared, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordHistogram(
                    Constants.Metrics.QueueOperationDurationMs, sw.ElapsedMilliseconds, [(Constants.Metrics.Tags.Operation, "clear"), (Constants.Metrics.Tags.Queue, queueName)]);
            }

            return true;
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "Failed to clear queue {QueueName}", queueName);

            // Final fallback
            try {
                await _publishChannel!.QueuePurgeAsync(queueName, ct).ConfigureAwait(false);
                _logger.LogInformation("Cleared queue {QueueName} using channel purge fallback", queueName);
                if (_options.EnableMetrics)
                    _metrics.IncrementCounter(Constants.Metrics.QueueCleared, 1, [(Constants.Metrics.Tags.Queue, queueName)]);

                return true;
            }
            catch (Exception purgeEx) {
                _logger.LogError(purgeEx, "Failed to clear queue {QueueName} using channel purge fallback", queueName);
                if (_options.EnableMetrics) {
                    _metrics.IncrementCounter(Constants.Metrics.QueueOperationFailed, 1, [(Constants.Metrics.Tags.Operation, "clear"), (Constants.Metrics.Tags.Queue, queueName)]);
                    _metrics.RecordError(Constants.Metrics.QueueOperationDuration, ex, [(Constants.Metrics.Tags.Operation, "clear"), (Constants.Metrics.Tags.Queue, queueName)]);
                }

                return false;
            }
        }
    }

    public async Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName) || string.IsNullOrWhiteSpace(exchangeName))
            return false;

        if (!IsConnected()) {
            _logger.LogWarning("Cannot bind queue {QueueName}: not connected", queueName);
            return false;
        }

        try {
            await _publishChannel!.QueueBindAsync(queueName, exchangeName, routingKey, cancellationToken: ct).ConfigureAwait(false);
            _logger.LogInformation("Bound queue {QueueName} to exchange {ExchangeName} with routing key {RoutingKey}", queueName, exchangeName, routingKey);
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to bind queue {QueueName} to exchange {ExchangeName}", queueName, exchangeName);
            return false;
        }
    }

    public async Task<bool> SendToQueue(string queueName, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot send to queue: queue name is null or empty");
            return false;
        }

        if (data.Length == 0) {
            _logger.LogWarning("Cannot send to queue {QueueName}: data is null or empty", queueName);
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot send to queue {QueueName}: not connected", queueName);
            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueFailure, 1, [(Constants.Metrics.Tags.Queue, queueName), (Constants.Metrics.Tags.Reason, "not_connected")]);

            return false;
        }

        using var timer = _metrics.StartTimer(Constants.Metrics.SendToQueueDuration, [(Constants.Metrics.Tags.Queue, queueName)]);
        var sw = Stopwatch.StartNew();
        try {
            await _publishChannel!.BasicPublishAsync(string.Empty, queueName, false, CreateBasicProperties(), data).ConfigureAwait(false);
            sw.Stop();
            _logger.LogDebug("Sent message to queue {QueueName} ({Size} bytes)", queueName, data.Length);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueSuccess, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordGauge(Constants.Metrics.SendToQueueMessageSizeBytes, data.Length, [(Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordHistogram(Constants.Metrics.SendToQueueDurationMs, sw.ElapsedMilliseconds, [(Constants.Metrics.Tags.Queue, queueName)]);
            }

            return true;
        }
        catch (PublishException ex) {
            // Publisher confirms are enabled and the broker nacked (or never confirmed) the publish.
            sw.Stop();
            _logger.LogError(ex, "Publish to queue {QueueName} was not confirmed by the broker", queueName);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.PublishUnconfirmed, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueFailure, 1, [(Constants.Metrics.Tags.Queue, queueName), (Constants.Metrics.Tags.Reason, "unconfirmed")]);
            }

            return false;
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "Failed to send message to queue {QueueName}", queueName);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueFailure, 1, [(Constants.Metrics.Tags.Queue, queueName), (Constants.Metrics.Tags.Reason, "exception")]);
                _metrics.RecordError(Constants.Metrics.SendToQueueDuration, ex, [(Constants.Metrics.Tags.Queue, queueName)]);
            }

            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendToQueueWithPriority(string queueName, byte[] data, byte priority)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot send to queue: queue name is null or empty");
            return false;
        }

        if (data.Length == 0) {
            _logger.LogWarning("Cannot send to queue {QueueName}: data is null or empty", queueName);
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot send to queue {QueueName}: not connected", queueName);
            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueFailure, 1, [(Constants.Metrics.Tags.Queue, queueName), (Constants.Metrics.Tags.Reason, "not_connected")]);

            return false;
        }

        try {
            var properties = CreateBasicProperties();
            properties.Priority = priority;
            await _publishChannel!.BasicPublishAsync(string.Empty, queueName, false, properties, data).ConfigureAwait(false);
            _logger.LogDebug("Sent message to queue {QueueName} with priority {Priority} ({Size} bytes)", queueName, priority, data.Length);
            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueSuccess, 1, [(Constants.Metrics.Tags.Queue, queueName)]);

            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to send prioritized message to queue {QueueName}", queueName);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueFailure, 1, [(Constants.Metrics.Tags.Queue, queueName), (Constants.Metrics.Tags.Reason, "exception")]);
                _metrics.RecordError(Constants.Metrics.SendToQueueDuration, ex, [(Constants.Metrics.Tags.Queue, queueName)]);
            }

            return false;
        }
    }

    public async Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(exchangeName)) {
            _logger.LogWarning("Cannot send to exchange: exchange name is null or empty");
            return false;
        }

        if (data.Length == 0) {
            _logger.LogWarning("Cannot send to exchange {ExchangeName}: data is null or empty", exchangeName);
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot send to exchange {ExchangeName}: not connected", exchangeName);
            if (_options.EnableMetrics) {
                var failureTags = new[] {
                    (Constants.Metrics.Tags.Exchange, exchangeName), (Constants.Metrics.Tags.RoutingKey, routingKey), (Constants.Metrics.Tags.Reason, "not_connected")
                };

                _metrics.IncrementCounter(Constants.Metrics.SendToExchangeFailure, 1, failureTags);
            }

            return false;
        }

        var tags = new[] { (Constants.Metrics.Tags.Exchange, exchangeName), (Constants.Metrics.Tags.RoutingKey, routingKey) };
        using var timer = _metrics.StartTimer(Constants.Metrics.SendToExchangeDuration, tags);
        var sw = Stopwatch.StartNew();
        try {
            await _publishChannel!.BasicPublishAsync(exchangeName, routingKey, false, CreateBasicProperties(), data).ConfigureAwait(false);
            sw.Stop();
            _logger.LogDebug("Published message to exchange {ExchangeName} with routing key {RoutingKey} ({Size} bytes)", exchangeName, routingKey, data.Length);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.SendToExchangeSuccess, 1, tags);
                _metrics.RecordGauge(Constants.Metrics.SendToExchangeMessageSizeBytes, data.Length, tags);
                _metrics.RecordHistogram(Constants.Metrics.SendToExchangeDurationMs, sw.ElapsedMilliseconds, tags);
            }

            return true;
        }
        catch (PublishException ex) {
            sw.Stop();
            _logger.LogError(ex, "Publish to exchange {ExchangeName} with routing key {RoutingKey} was not confirmed by the broker", exchangeName, routingKey);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.PublishUnconfirmed, 1, tags);
                _metrics.IncrementCounter(Constants.Metrics.SendToExchangeFailure, 1, tags);
            }

            return false;
        }
        catch (Exception ex) {
            sw.Stop();
            _logger.LogError(ex, "Failed to publish message to exchange {ExchangeName} with routing key {RoutingKey}", exchangeName, routingKey);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.SendToExchangeFailure, 1, tags);
                _metrics.RecordError(Constants.Metrics.SendToExchangeDuration, ex, tags);
            }

            return false;
        }
    }

    public async Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            return [];

        var vhost = _options.VirtualHost == "/" ? "%2F" : Uri.EscapeDataString(_options.VirtualHost);
        var request = new {
            count = Math.Max(1, maxMessages),
            ackmode = "ack_requeue_true",
            encoding = "auto",
            truncate = 50_000
        };

        using var response = await _httpClient.PostAsync(
                $"queues/{vhost}/{Uri.EscapeDataString(queueName)}/get",
                new StringContent(JsonSerializer.Serialize(request, _serializerOptions), Encoding.UTF8, FileTypeInfo.Json.MimeType), ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var results = new List<QueuePeekMessage>();
        foreach (var el in doc.RootElement.EnumerateArray()) {
            results.Add(
                new(
                    el.TryGetProperty("payload", out var payload) ? payload.ToString() : string.Empty,
                    el.TryGetProperty("payload_encoding", out var payloadEncoding) ? payloadEncoding.GetString() : null,
                    el.TryGetProperty("exchange", out var exchange) ? exchange.GetString() : null,
                    el.TryGetProperty("routing_key", out var routingKey) ? routingKey.GetString() : null,
                    el.TryGetProperty("message_count", out var messageCount) ? messageCount.GetInt64() : null,
                    el.TryGetProperty("redelivered", out var redelivered) && redelivered.GetBoolean()));
        }

        return results;
    }

    /// <summary>
    /// Sends a message that becomes visible on <paramref name="queueName" /> only after <paramref name="delay" /> has elapsed. Implemented with a companion wait queue (
    /// <c>{queueName}.wait</c>) whose dead-letter target is the real queue: the message is published to the wait queue with a per-message TTL, and when the TTL fires the broker
    /// dead-letters it onto the destination. No broker plugin required.
    /// </summary>
    public async Task<bool> SendToQueueDelayed(string queueName, byte[] data, TimeSpan delay, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot send delayed message: queue name is null or empty");
            return false;
        }

        if (delay <= TimeSpan.Zero)
            return await SendToQueue(queueName, data).ConfigureAwait(false);

        if (!IsConnected()) {
            _logger.LogWarning("Cannot send delayed message to queue {QueueName}: not connected", queueName);
            return false;
        }

        var waitQueue = queueName + Constants.WaitQueueSuffix;
        try {
            if (!_declaredWaitQueues.ContainsKey(waitQueue)) {
                var arguments = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = string.Empty, ["x-dead-letter-routing-key"] = queueName };
                await _publishChannel!.QueueDeclareAsync(waitQueue, true, false, false, arguments, cancellationToken: ct).ConfigureAwait(false);
                _declaredWaitQueues.TryAdd(waitQueue, 0);
                _logger.LogDebug("Declared delay wait queue {WaitQueue} dead-lettering to {QueueName}", waitQueue, queueName);
            }

            var properties = CreateBasicProperties();
            properties.Expiration = ((long)delay.TotalMilliseconds).ToString();
            await _publishChannel!.BasicPublishAsync(string.Empty, waitQueue, false, properties, data, ct).ConfigureAwait(false);
            _logger.LogDebug("Sent delayed message to queue {QueueName} via {WaitQueue} (delay: {Delay})", queueName, waitQueue, delay);
            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueDelayed, 1, [(Constants.Metrics.Tags.Queue, queueName)]);

            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to send delayed message to queue {QueueName} via {WaitQueue}", queueName, waitQueue);
            if (_options.EnableMetrics)
                _metrics.IncrementCounter(
                    Constants.Metrics.SendToQueueFailure, 1, [(Constants.Metrics.Tags.Queue, queueName), (Constants.Metrics.Tags.Reason, "delayed_exception")]);

            return false;
        }
    }

    public async Task<bool> CreateQueueWithDlq(
        string queueName,
        bool durable = true,
        string? dlqName = null,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot create queue with DLQ: queue name is null or empty");
            return false;
        }

        dlqName ??= queueName + Constants.DeadLetterQueueSuffix;
        if (!await CreateQueue(dlqName, durable, false, false, null, ct).ConfigureAwait(false))
            return false;

        var mainArguments = arguments != null ? new Dictionary<string, object>(arguments) : new Dictionary<string, object>(2);
        mainArguments["x-dead-letter-exchange"] = string.Empty;
        mainArguments["x-dead-letter-routing-key"] = dlqName;
        var created = await CreateQueue(queueName, durable, false, false, mainArguments, ct).ConfigureAwait(false);
        if (!created) {
            // The most common failure is a PRECONDITION_FAILED redeclare: RabbitMQ cannot change arguments on an existing queue.
            _logger.LogError(
                "Failed to declare queue {QueueName} with dead-letter arguments. If the queue already exists without them, delete and recreate it (RabbitMQ cannot alter arguments on an existing queue)",
                queueName);
        }

        return created;
    }

    public async Task<MessageQueueInfo?> GetQueueInfoAsync(string queueName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            return null;

        try {
            var vhost = _options.VirtualHost == "/" ? "%2F" : Uri.EscapeDataString(_options.VirtualHost);
            using var response = await _httpClient.GetAsync($"queues/{vhost}/{Uri.EscapeDataString(queueName)}", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) {
                _logger.LogDebug("Queue info request for {QueueName} returned {StatusCode}", queueName, response.StatusCode);
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            return ParseQueueInfo(doc.RootElement);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to get queue info for {QueueName}", queueName);
            return null;
        }
    }

    public async Task<IReadOnlyList<MessageQueueInfo>> GetAllQueuesInfoAsync(CancellationToken ct = default)
    {
        try {
            var vhost = _options.VirtualHost == "/" ? "%2F" : Uri.EscapeDataString(_options.VirtualHost);
            using var response = await _httpClient.GetAsync($"queues/{vhost}", ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var results = new List<MessageQueueInfo>();
            foreach (var el in doc.RootElement.EnumerateArray())
                results.Add(ParseQueueInfo(el));

            return results;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to get queue info for all queues");
            return [];
        }
    }

    public async Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot subscribe to queue: queue name is null or empty");
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot subscribe to queue {QueueName}: not connected", queueName);
            return false;
        }

        if (_consumers.ContainsKey(queueName)) {
            _logger.LogWarning("Already subscribed to queue {QueueName}", queueName);
            return false;
        }

        try {
            // Create a dedicated channel for this subscription (one channel per subscriber). The dispatch concurrency controls how many
            // handler invocations the client runs in parallel on this channel (the 7.x default is 1, i.e. strictly sequential).
            var limit = _options.GetProcessingLimit(queueName);
            var dispatchConcurrency = limit > 0 ? (ushort)Math.Min(limit, ushort.MaxValue) : Constants.UnlimitedDispatchConcurrency;
            var channelOptions = new CreateChannelOptions(false, false, consumerDispatchConcurrency: dispatchConcurrency);
            var subscriptionChannel = await _connection!.CreateChannelAsync(channelOptions, ct).ConfigureAwait(false);
            await AssertQueueExistsForSubscriptionAsync(subscriptionChannel, queueName, ct).ConfigureAwait(false);

            // Broker-side backpressure: only `limit` unacked messages are delivered to this consumer at a time; the rest stay on the server.
            if (limit > 0)
                await subscriptionChannel.BasicQosAsync(0, (ushort)Math.Min(limit, ushort.MaxValue), false, ct).ConfigureAwait(false);

            // In-process guarantee on top of prefetch/dispatch limits.
            if (limit > 0 && !_processingSemaphores.ContainsKey(queueName))
                _processingSemaphores[queueName] = new(limit, limit);

            var consumer = new AsyncEventingBasicConsumer(subscriptionChannel);
            consumer.ReceivedAsync += async (_, args) => await HandleMessageAsync(queueName, args, onMessage, subscriptionChannel, ct).ConfigureAwait(false);
            var consumerTag = await subscriptionChannel.BasicConsumeAsync(queueName, false, consumer, ct).ConfigureAwait(false);
            _consumers[queueName] = (consumer, consumerTag, subscriptionChannel);
            _logger.LogInformation(
                "Subscribed to queue {QueueName} with consumer tag {ConsumerTag} on dedicated channel (concurrency limit: {Limit})", queueName, consumerTag,
                limit > 0 ? limit : "unlimited");

            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.QueueSubscribed, 1, [(Constants.Metrics.Tags.Queue, queueName)]);

            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to subscribe to queue {QueueName}", queueName);
            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.QueueSubscriptionFailed, 1, [(Constants.Metrics.Tags.Queue, queueName)]);

            return false;
        }
    }

    /// <summary>Builds the publish properties applied to every outgoing message: persistence per <see cref="RabbitMqOptions.PersistentMessages" />, a message id, and a UTC timestamp.</summary>
    private BasicProperties CreateBasicProperties()
        => new() { Persistent = _options.PersistentMessages, MessageId = Guid.NewGuid().ToString("D"), Timestamp = new(DateTimeOffset.UtcNow.ToUnixTimeSeconds()) };

    /// <summary>Maps a management API queue object to <see cref="MessageQueueInfo" />. Rates (when present) land in AdditionalProperties as messages/sec.</summary>
    private static MessageQueueInfo ParseQueueInfo(JsonElement el)
    {
        var additional = new Dictionary<string, object>();
        if (el.TryGetProperty("messages_details", out var messagesDetails) && messagesDetails.TryGetProperty("rate", out var messagesRate))
            additional["messages_rate"] = messagesRate.GetDouble();

        if (el.TryGetProperty("message_stats", out var stats)) {
            if (stats.TryGetProperty("publish_details", out var publishDetails) && publishDetails.TryGetProperty("rate", out var publishRate))
                additional["publish_rate"] = publishRate.GetDouble();

            if (stats.TryGetProperty("deliver_get_details", out var deliverDetails) && deliverDetails.TryGetProperty("rate", out var deliverRate))
                additional["deliver_rate"] = deliverRate.GetDouble();
        }

        if (el.TryGetProperty("memory", out var memory) && memory.ValueKind == JsonValueKind.Number)
            additional["memory_bytes"] = memory.GetInt64();

        return new(
            el.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty, el.TryGetProperty("state", out var state) ? state.GetString() : null,
            el.TryGetProperty("type", out var type) ? type.GetString() : null,
            el.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Number ? messages.GetInt64() : 0,
            el.TryGetProperty("messages_ready", out var ready) && ready.ValueKind == JsonValueKind.Number ? ready.GetInt64() : 0,
            el.TryGetProperty("messages_unacknowledged", out var unacked) && unacked.ValueKind == JsonValueKind.Number ? unacked.GetInt64() : 0,
            el.TryGetProperty("consumers", out var consumers) && consumers.ValueKind == JsonValueKind.Number ? consumers.GetInt32() : 0, additional);
    }

    /// <summary>
    /// Verifies the queue already exists (passive declare). Subscribers never create queues — provision them at host startup via <see cref="CreateQueue" /> or
    /// <c>DefinedQueues</c>.
    /// </summary>
    private static async Task AssertQueueExistsForSubscriptionAsync(IChannel channel, string queueName, CancellationToken ct)
    {
        try {
            await channel.QueueDeclareAsync(queueName, true, false, false, null, true, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404) {
            throw new NotFoundException(
                $"Queue '{queueName}' does not exist. Declare it at application startup before subscribing — workers must not create queues on register/start.", ex);
        }
    }

    private async ValueTask ConnectCoreAsync(CancellationToken ct)
    {
        try {
            _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}", _options.Host, _options.Port);
            _connection = await _connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
            _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            _connection.RecoverySucceededAsync += OnRecoverySucceededAsync;

            // Create a shared channel for publishing and queue management operations. With publisher confirms enabled,
            // BasicPublishAsync only completes once the broker confirms the publish (and throws on nack).
            var publishChannelOptions = _options.PublisherConfirms ? new CreateChannelOptions(true, true) : null;
            _publishChannel = await _connection.CreateChannelAsync(publishChannelOptions, ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Successfully connected to RabbitMQ (PublisherConfirms: {Confirms}, AutomaticRecovery: {Recovery})", _options.PublisherConfirms, _options.AutomaticRecovery);

            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.ConnectionEstablished, 1);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error establishing RabbitMQ connection");
            if (_options.EnableMetrics)
                _metrics.IncrementCounter(Constants.Metrics.ConnectionFailed, 1);

            throw;
        }
    }

    private async Task InitializeDefinedQueues(CancellationToken ct)
    {
        if (!IsConnected()) {
            _logger.LogWarning("Cannot initialize defined queues: not connected");
            return;
        }

        _logger.LogInformation("Initializing {Count} defined queues", _options.DefinedQueues!.Count);
        var successCount = 0;
        var failureCount = 0;
        foreach (var queueName in _options.DefinedQueues!) {
            try {
                var result = await CreateQueue(queueName, true, false, false, null, ct).ConfigureAwait(false);
                if (result) {
                    successCount++;
                    _logger.LogDebug("Initialized defined queue: {QueueName}", queueName);
                }
                else
                    failureCount++;
            }
            catch (Exception ex) {
                failureCount++;
                _logger.LogError(ex, "Failed to initialize defined queue: {QueueName}", queueName);
            }
        }

        _logger.LogInformation("Queue initialization complete: {Success} succeeded, {Failure} failed", successCount, failureCount);
    }

    private async Task CancelAllConsumersAsync(CancellationToken ct)
    {
        if (_consumers.Count == 0)
            return;

        _logger.LogDebug("Cancelling {Count} active consumers", _consumers.Count);
        var cancellationTasks = new List<Task>();
        foreach (var kvp in _consumers.ToList()) {
            var queueName = kvp.Key;
            var consumerTag = kvp.Value.ConsumerTag;
            var channel = kvp.Value.Channel;
            cancellationTasks.Add(
                Task.Run(
                    async () => {
                        try {
                            await channel.BasicCancelAsync(consumerTag, cancellationToken: ct).ConfigureAwait(false);
                            _logger.LogDebug("Cancelled consumer {ConsumerTag} for queue {QueueName}", consumerTag, queueName);

                            // Close and dispose the subscription channel
                            try {
                                await channel.CloseAsync(ct).ConfigureAwait(false);
                                await channel.DisposeAsync().ConfigureAwait(false);
                                _logger.LogDebug("Closed channel for queue {QueueName}", queueName);
                            }
                            catch (Exception ex) {
                                _logger.LogWarning(ex, "Error closing channel for queue {QueueName}", queueName);
                            }
                        }
                        catch (Exception ex) {
                            _logger.LogWarning(ex, "Error cancelling consumer {ConsumerTag} for queue {QueueName}", consumerTag, queueName);

                            // Still try to close the channel even if cancel failed
                            try {
                                await channel.CloseAsync(ct).ConfigureAwait(false);
                                await channel.DisposeAsync().ConfigureAwait(false);
                            }
                            catch {
                                // Ignore errors during cleanup
                            }
                        }
                    }, ct));
        }

        await Task.WhenAll(cancellationTasks).ConfigureAwait(false);
        _consumers.Clear();
    }

    private void DisposeProcessingSemaphores()
    {
        foreach (var semaphore in _processingSemaphores.Values) {
            try {
                semaphore.Dispose();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error disposing processing semaphore");
            }
        }

        _processingSemaphores.Clear();
    }

    private async Task HandleMessageAsync(string queueName, BasicDeliverEventArgs args, Func<byte[], Task<bool>> onMessage, IChannel channel, CancellationToken ct)
    {
        SemaphoreSlim? semaphore = null;
        var semaphoreAcquired = false;
        var messageId = Guid.NewGuid().ToString("N").Substring(0, 8); // Short ID for logging
        try {
            // Acquire semaphore if a (per-queue or global) processing limit is enabled
            if (_processingSemaphores.TryGetValue(queueName, out semaphore)) {
                try {
                    await semaphore.WaitAsync(ct).ConfigureAwait(false);
                    semaphoreAcquired = true;
                }
                catch (OperationCanceledException) {
                    _logger.LogWarning("Processing cancelled for message {MessageId} from queue {QueueName}, requeuing", messageId, queueName);
                    await SafeNackAsync(channel, args.DeliveryTag, true, ct).ConfigureAwait(false);
                    return;
                }
            }

            var messageData = args.Body.ToArray();
            _logger.LogDebug("Processing message {MessageId} from queue {QueueName} ({Size} bytes)", messageId, queueName, messageData.Length);
            using var timer = _metrics.StartTimer(Constants.Metrics.MessageProcessingDuration, [(Constants.Metrics.Tags.Queue, queueName)]);
            var sw = Stopwatch.StartNew();
            try {
                var shouldRequeue = await onMessage(messageData).ConfigureAwait(false);
                sw.Stop();
                if (shouldRequeue) {
                    await SafeAckAsync(channel, args.DeliveryTag, false, true, ct).ConfigureAwait(false);
                    _logger.LogDebug("Message {MessageId} from queue {QueueName} requeued by handler", messageId, queueName);
                    if (_options.EnableMetrics)
                        _metrics.IncrementCounter(Constants.Metrics.MessageRequeued, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                }
                else {
                    await SafeAckAsync(channel, args.DeliveryTag, true, false, ct).ConfigureAwait(false);
                    _logger.LogDebug("Message {MessageId} from queue {QueueName} acknowledged", messageId, queueName);
                    if (_options.EnableMetrics) {
                        _metrics.IncrementCounter(Constants.Metrics.MessageProcessed, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                        _metrics.RecordHistogram(Constants.Metrics.MessageProcessingDurationMs, sw.ElapsedMilliseconds, [(Constants.Metrics.Tags.Queue, queueName)]);
                    }
                }
            }
            catch (Exception ex) {
                sw.Stop();
                await HandleMessageExceptionAsync(queueName, messageId, args.DeliveryTag, ex, channel, ct).ConfigureAwait(false);
            }
        }
        finally {
            if (semaphoreAcquired && semaphore != null)
                semaphore.Release();
        }
    }

    private async Task HandleMessageExceptionAsync(string queueName, string messageId, ulong deliveryTag, Exception ex, IChannel channel, CancellationToken ct = default)
    {
        _logger.LogError(ex, "Error processing message {MessageId} from queue {QueueName}", messageId, queueName);
        if (_options.EnableMetrics) {
            _metrics.IncrementCounter(Constants.Metrics.MessageProcessingFailed, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
            _metrics.RecordError(Constants.Metrics.MessageProcessingDuration, ex, [(Constants.Metrics.Tags.Queue, queueName)]);
        }

        switch (_options.ExceptionHandling) {
            case MessageProcessingExceptionHandling.IgnoreAndRemoveFromQueue:
                await SafeAckAsync(channel, deliveryTag, true, false, ct).ConfigureAwait(false);
                _logger.LogWarning("Message {MessageId} removed from queue {QueueName} due to exception (IgnoreAndRemoveFromQueue)", messageId, queueName);
                break;
            case MessageProcessingExceptionHandling.ThrowAndRemoveFromQueue:
                await SafeAckAsync(channel, deliveryTag, true, false, ct).ConfigureAwait(false);
                _logger.LogWarning("Message {MessageId} removed from queue {QueueName} due to exception (ThrowAndRemoveFromQueue)", messageId, queueName);
                // Honor the documented contract: the message is acked (removed) and the original exception is rethrown.
                // The RabbitMQ client routes consumer callback exceptions to the connection's CallbackExceptionAsync handler; the consumer keeps running.
                ExceptionDispatchInfo.Capture(ex).Throw();
                break;
            case MessageProcessingExceptionHandling.RequeueOnException:
            default:
                await SafeNackAsync(channel, deliveryTag, true, ct).ConfigureAwait(false);
                _logger.LogWarning("Message {MessageId} requeued due to exception (RequeueOnException)", messageId);
                break;
        }
    }

    private async Task SafeAckAsync(IChannel channel, ulong deliveryTag, bool ack, bool requeue, CancellationToken ct = default)
    {
        try {
            if (ack)
                await channel.BasicAckAsync(deliveryTag, false, ct).ConfigureAwait(false);
            else
                await channel.BasicNackAsync(deliveryTag, false, requeue, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to acknowledge message with delivery tag {DeliveryTag}", deliveryTag);
        }
    }

    private async Task SafeNackAsync(IChannel channel, ulong deliveryTag, bool requeue, CancellationToken ct = default)
    {
        try {
            await channel.BasicNackAsync(deliveryTag, false, requeue, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to nack message with delivery tag {DeliveryTag}", deliveryTag);
        }
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs shutdownEventArgs)
    {
        if (shutdownEventArgs.Initiator == ShutdownInitiator.Application) {
            if (_connection != null)
                _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;

            _logger.LogInformation("RabbitMQ connection closed by application");
            return Task.CompletedTask;
        }

        _logger.LogWarning("RabbitMQ connection shutdown detected: {Reason}", shutdownEventArgs.ReplyText);
        if (_options.EnableMetrics)
            _metrics.IncrementCounter(Constants.Metrics.ConnectionLost, 1);

        // With automatic recovery enabled the client reconnects and restores channels + consumers on its own,
        // so consumer bookkeeping must be kept. Without recovery the channels are permanently dead — clear them.
        if (!_options.AutomaticRecovery) {
            if (_connection != null)
                _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;

            var consumerCount = _consumers.Count;
            _consumers.Clear();
            _declaredWaitQueues.Clear();
            if (consumerCount > 0)
                _logger.LogInformation("Cleared {Count} consumers due to connection loss (automatic recovery disabled)", consumerCount);
        }
        else
            _logger.LogInformation("Waiting for RabbitMQ automatic recovery ({Count} consumers will be restored)", _consumers.Count);

        return Task.CompletedTask;
    }

    private Task OnRecoverySucceededAsync(object sender, AsyncEventArgs args)
    {
        _logger.LogInformation("RabbitMQ connection recovered; {Count} consumers restored", _consumers.Count);
        if (_options.EnableMetrics)
            _metrics.IncrementCounter(Constants.Metrics.ConnectionRecovered, 1);

        return Task.CompletedTask;
    }
}