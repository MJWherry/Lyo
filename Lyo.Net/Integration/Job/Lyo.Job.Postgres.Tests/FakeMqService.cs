using Lyo.Health;
using Lyo.MessageQueue;
using Lyo.MessageQueue.RabbitMq;

namespace Lyo.Job.Postgres.Tests;

/// <summary>Fake RabbitMQ service for integration tests - always connected, SendToQueue/SendToExchange succeed.</summary>
public sealed class FakeMqService : IRabbitMqService
{
    private bool _connected = true;

    public bool IsConnected() => _connected;

    public Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> DeleteExchange(string exchangeName, bool ifUnused = false, CancellationToken ct = default) => Task.FromResult(true);

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

    public Task<bool> CreateQueue(
        string queueName,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> ClearQueue(string queueName, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> SendToQueue(string queueName, byte[] data) => Task.FromResult(true);

    public Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data) => Task.FromResult(true);

    public Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<QueuePeekMessage>>([]);

    public Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default) => Task.FromResult(true);

    public string HealthCheckName => "fake-mq";

    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero, null, new Dictionary<string, object?> { ["fake"] = true }));

    public void SetConnected(bool value) => _connected = value;
}