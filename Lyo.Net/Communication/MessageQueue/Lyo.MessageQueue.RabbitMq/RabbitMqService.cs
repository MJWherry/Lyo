using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lyo.MessageQueue.RabbitMq;

/// <summary>RabbitMQ implementation of the message queue service interface. Provides robust error handling, metrics, logging, and connection management.</summary>
public sealed class RabbitMqService : IRabbitMqService
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private readonly Dictionary<string, (AsyncEventingBasicConsumer Consumer, string ConsumerTag, IChannel Channel)> _consumers = new();
    private readonly HttpClient _httpClient;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly IMetrics _metrics;
    private readonly RabbitMqOptions _options;
    private readonly Dictionary<string, SemaphoreSlim> _processingSemaphores = new();
    private readonly JsonSerializerOptions _serializerOptions;

    private IConnection? _connection;
    private bool _disposed;
    private IChannel? _publishChannel; // Shared channel for publishing and queue management operations

    //public IConnection? Connection => _connection;
    //public IChannel? Channel => _channel;

    public RabbitMqService(
        RabbitMqOptions options,
        IConnectionFactory connectionFactory,
        HttpClient? httpClient = null,
        ILogger<RabbitMqService>? logger = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNull(connectionFactory, nameof(connectionFactory));
        _options = options;
        _connectionFactory = connectionFactory;
        _logger = logger ?? NullLogger<RabbitMqService>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _serializerOptions = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress ??= new($"{_options.AdminUrl}/api/");
        _httpClient.DefaultRequestHeaders.Authorization ??= new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}")));
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
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqService));

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (IsConnected()) {
                _logger.LogDebug("Already connected to RabbitMQ");
                return;
            }

            await ConnectCoreAsync(ct).ConfigureAwait(false);

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
        if (string.IsNullOrWhiteSpace(exchangeName)) {
            _logger.LogWarning("Cannot create exchange: exchange name is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(exchangeType))
            exchangeType = "direct";

        if (!IsConnected()) {
            _logger.LogWarning("Cannot create exchange {ExchangeName}: not connected", exchangeName);
            return false;
        }

        try {
            await _publishChannel!.ExchangeDeclareAsync(exchangeName, exchangeType, durable, autoDelete, arguments, cancellationToken: ct).ConfigureAwait(false);
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

        using var timer = _metrics.StartTimer(Constants.Metrics.QueueOperationDuration, [(Constants.Metrics.Tags.Operation, "create"), (Constants.Metrics.Tags.Queue, queueName)]);
        var sw = Stopwatch.StartNew();
        try {
            var result = await _publishChannel!.QueueDeclareAsync(queueName, durable, exclusive, autoDelete, arguments, cancellationToken: ct).ConfigureAwait(false);

            // Initialize processing semaphore for this queue if processing limit is set
            if (_options.ProcessingLimit > 0 && !_processingSemaphores.ContainsKey(queueName)) {
                _processingSemaphores[queueName] = new(_options.ProcessingLimit, _options.ProcessingLimit);
                _logger.LogDebug("Created processing semaphore for queue {QueueName} with limit {Limit}", queueName, _options.ProcessingLimit);
            }

            sw.Stop();
            _logger.LogInformation("Created queue {QueueName} (Durable: {Durable}, Exclusive: {Exclusive}, AutoDelete: {AutoDelete})", queueName, durable, exclusive, autoDelete);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.QueueCreated, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordHistogram(
                    Constants.Metrics.QueueOperationDurationMs, sw.ElapsedMilliseconds, [(Constants.Metrics.Tags.Operation, "create"), (Constants.Metrics.Tags.Queue, queueName)]);
            }

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
            await _publishChannel!.QueueBindAsync(queueName, exchangeName, routingKey ?? "", cancellationToken: ct).ConfigureAwait(false);
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

        if (data == null || data.Length == 0) {
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
            await _publishChannel!.BasicPublishAsync(string.Empty, queueName, data).ConfigureAwait(false);
            sw.Stop();
            _logger.LogDebug("Sent message to queue {QueueName} ({Size} bytes)", queueName, data.Length);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.SendToQueueSuccess, 1, [(Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordGauge(Constants.Metrics.SendToQueueMessageSizeBytes, data.Length, [(Constants.Metrics.Tags.Queue, queueName)]);
                _metrics.RecordHistogram(Constants.Metrics.SendToQueueDurationMs, sw.ElapsedMilliseconds, [(Constants.Metrics.Tags.Queue, queueName)]);
            }

            return true;
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

    public async Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(exchangeName)) {
            _logger.LogWarning("Cannot send to exchange: exchange name is null or empty");
            return false;
        }

        if (data == null || data.Length == 0) {
            _logger.LogWarning("Cannot send to exchange {ExchangeName}: data is null or empty", exchangeName);
            return false;
        }

        if (!IsConnected()) {
            _logger.LogWarning("Cannot send to exchange {ExchangeName}: not connected", exchangeName);
            if (_options.EnableMetrics) {
                var failureTags = new[] {
                    (Constants.Metrics.Tags.Exchange, exchangeName), (Constants.Metrics.Tags.RoutingKey, routingKey ?? ""), (Constants.Metrics.Tags.Reason, "not_connected")
                };

                _metrics.IncrementCounter(Constants.Metrics.SendToExchangeFailure, 1, failureTags);
            }

            return false;
        }

        var tags = new[] { (Constants.Metrics.Tags.Exchange, exchangeName), (Constants.Metrics.Tags.RoutingKey, routingKey ?? "") };
        using var timer = _metrics.StartTimer(Constants.Metrics.SendToExchangeDuration, tags);
        var sw = Stopwatch.StartNew();
        try {
            await _publishChannel!.BasicPublishAsync(exchangeName, routingKey ?? "", data).ConfigureAwait(false);
            sw.Stop();
            _logger.LogDebug("Published message to exchange {ExchangeName} with routing key {RoutingKey} ({Size} bytes)", exchangeName, routingKey, data.Length);
            if (_options.EnableMetrics) {
                _metrics.IncrementCounter(Constants.Metrics.SendToExchangeSuccess, 1, tags);
                _metrics.RecordGauge(Constants.Metrics.SendToExchangeMessageSizeBytes, data.Length, tags);
                _metrics.RecordHistogram(Constants.Metrics.SendToExchangeDurationMs, sw.ElapsedMilliseconds, tags);
            }

            return true;
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
                $"queues/{vhost}/{Uri.EscapeDataString(queueName)}/get", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), ct)
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

    public async Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queueName)) {
            _logger.LogWarning("Cannot subscribe to queue: queue name is null or empty");
            return false;
        }

        if (onMessage == null) {
            _logger.LogWarning("Cannot subscribe to queue {QueueName}: message handler is null", queueName);
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
            // Create a dedicated channel for this subscription (one channel per subscriber)
            var subscriptionChannel = await _connection!.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);

            // Ensure queue exists on the subscription channel
            await subscriptionChannel.QueueDeclareAsync(queueName, true, false, false, cancellationToken: ct).ConfigureAwait(false);

            // Initialize processing semaphore for this queue if processing limit is set
            if (_options.ProcessingLimit > 0 && !_processingSemaphores.ContainsKey(queueName))
                _processingSemaphores[queueName] = new(_options.ProcessingLimit, _options.ProcessingLimit);

            var consumer = new AsyncEventingBasicConsumer(subscriptionChannel);
            consumer.ReceivedAsync += async (sender, args) => await HandleMessageAsync(queueName, args, onMessage, subscriptionChannel, ct).ConfigureAwait(false);
            var consumerTag = await subscriptionChannel.BasicConsumeAsync(queueName, false, consumer, ct).ConfigureAwait(false);
            _consumers[queueName] = (consumer, consumerTag, subscriptionChannel);
            _logger.LogInformation("Subscribed to queue {QueueName} with consumer tag {ConsumerTag} on dedicated channel", queueName, consumerTag);
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

    private async ValueTask ConnectCoreAsync(CancellationToken ct)
    {
        try {
            _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}", _options.Host, _options.Port);
            _connection = await _connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
            _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            // Create a shared channel for publishing and queue management operations
            _publishChannel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
            _logger.LogInformation("Successfully connected to RabbitMQ");
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
            var consumer = kvp.Value.Consumer;
            var consumerTag = kvp.Value.ConsumerTag;
            var channel = kvp.Value.Channel;
            cancellationTasks.Add(
                Task.Run(
                    async () => {
                        try {
                            consumer.ReceivedAsync -= null; // Remove event handlers
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
            // Acquire semaphore if processing limit is enabled
            if (_options.ProcessingLimit > 0 && _processingSemaphores.TryGetValue(queueName, out semaphore)) {
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
                // Note: Exception is logged but not rethrown to prevent breaking the consumer
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
        if (_connection != null)
            _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;

        if (shutdownEventArgs.Initiator == ShutdownInitiator.Application) {
            _logger.LogInformation("RabbitMQ connection closed by application");
            return Task.CompletedTask;
        }

        _logger.LogWarning("RabbitMQ connection shutdown detected: {Reason}", shutdownEventArgs.ReplyText);
        if (_options.EnableMetrics)
            _metrics.IncrementCounter(Constants.Metrics.ConnectionLost, 1);

        // Clear all consumers since their channels are now invalid
        // Note: The channels will be automatically closed when the connection closes
        var consumerCount = _consumers.Count;
        _consumers.Clear();
        if (consumerCount > 0)
            _logger.LogInformation("Cleared {Count} consumers due to connection loss", consumerCount);

        return Task.CompletedTask;
    }
}