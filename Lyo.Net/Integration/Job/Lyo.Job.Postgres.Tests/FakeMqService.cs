using Lyo.Health;
using Lyo.MessageQueue;

namespace Lyo.Job.Postgres.Tests;

/// <summary>Fake MQ service for integration tests — always connected, all operations succeed.</summary>
public sealed class FakeMqService : IMqService
{
    private bool _connected = true;

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