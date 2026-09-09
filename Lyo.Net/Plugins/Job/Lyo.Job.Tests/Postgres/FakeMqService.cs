using System.Text.Json;
using Lyo.Health;
using Lyo.MessageQueue;

namespace Lyo.Job.Tests.Postgres;

/// <summary>Fake MQ service for integration tests. always connected, all operations succeed.</summary>
public sealed class FakeMqService : IMqService
{
    private bool _connected = true;
    private readonly Dictionary<string, List<QueuePeekMessage>> _peeked = new(StringComparer.Ordinal);

    public List<(string Name, string Type, bool Durable, bool AutoDelete)> CreatedExchanges { get; } = [];

    public List<(string Name, bool Durable, bool Exclusive, bool AutoDelete)> CreatedQueues { get; } = [];

    public List<(string QueueName, string ExchangeName, string RoutingKey)> Bindings { get; } = [];

    public bool IsConnected() => _connected;

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _connected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _connected = false;
        return Task.CompletedTask;
    }

    public Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
    {
        CreatedExchanges.Add((exchangeName, exchangeType, durable, autoDelete));
        return Task.FromResult(true);
    }

    public Task<bool> CreateQueue(
        string queueName,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
    {
        CreatedQueues.Add((queueName, durable, exclusive, autoDelete));
        return Task.FromResult(true);
    }

    public Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> ClearQueue(string queueName, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default)
    {
        Bindings.Add((queueName, exchangeName, routingKey));
        return Task.FromResult(true);
    }

    public Task<bool> SendToQueue(string queueName, byte[] data) => Task.FromResult(true);

    public Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data) => Task.FromResult(true);

    public Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default)
    {
        if (!_peeked.TryGetValue(queueName, out var messages))
            return Task.FromResult<IReadOnlyList<QueuePeekMessage>>([]);

        return Task.FromResult<IReadOnlyList<QueuePeekMessage>>(messages.Take(Math.Max(1, maxMessages)).ToArray());
    }

    /// <summary>Seeds peek results as enveloped run ids for the given queue (and optionally its <c>.wait</c> companion).</summary>
    public void SeedPeek(string queueName, params Guid[] runIds)
    {
        _peeked[queueName] = runIds.Select(id => {
                var envelope = new QueueMessageEnvelope<Guid>(id, 0, id.ToString("D"), DateTime.UtcNow);
                return new QueuePeekMessage(JsonSerializer.Serialize(envelope));
            })
            .ToList();
    }

    /// <summary>Handlers registered per queue, so a test can deliver a message the way the broker would.</summary>
    public Dictionary<string, Func<byte[], Task<bool>>> Handlers { get; } = new(StringComparer.Ordinal);

    public Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default)
    {
        lock (Handlers)
            Handlers[queueName] = onMessage;

        return Task.FromResult(true);
    }

    /// <summary>The handler for <paramref name="queueName" />, or the only registered handler when no name is given.</summary>
    public Func<byte[], Task<bool>>? GetHandler(string? queueName = null)
    {
        lock (Handlers) {
            if (queueName is not null)
                return Handlers.GetValueOrDefault(queueName);

            return Handlers.Count == 1 ? Handlers.Values.First() : null;
        }
    }

    public string HealthCheckName => "fake-mq";

    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero, null, new Dictionary<string, object?> { ["fake"] = true }));

    public void SetConnected(bool value) => _connected = value;
}