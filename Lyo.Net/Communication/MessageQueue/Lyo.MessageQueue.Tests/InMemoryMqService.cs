using System.Collections.Concurrent;
using System.Text;
using Lyo.Health;

namespace Lyo.MessageQueue.Tests;

/// <summary>In-memory IMqService implementation for unit testing without external dependencies.</summary>
public sealed class InMemoryMqService : IMqService, IDisposable
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<byte[]>> _queues = new();
    private bool _connected;

    public void Dispose() => _connected = false;

    public string HealthCheckName => "inmemory-mq";

    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero, null, new Dictionary<string, object?> { ["inmemory"] = true }));

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

    public bool IsConnected() => _connected;

    public Task<bool> CreateQueue(
        string queueName,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
    {
        _queues.TryAdd(queueName, new());
        return Task.FromResult(true);
    }

    public Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default)
        => Task.FromResult(_queues.TryRemove(queueName, out var _));

    public Task<bool> ClearQueue(string queueName, CancellationToken ct = default)
    {
        if (_queues.TryGetValue(queueName, out var queue)) {
            while (queue.TryDequeue(out var _)) { }

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> SendToQueue(string queueName, byte[] data)
    {
        var queue = _queues.GetOrAdd(queueName, _ => new());
        queue.Enqueue(data);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default)
    {
        if (!_queues.TryGetValue(queueName, out var queue))
            return Task.FromResult<IReadOnlyList<QueuePeekMessage>>([]);

        var results = new List<QueuePeekMessage>();
        foreach (var data in queue) {
            if (results.Count >= maxMessages)
                break;

            results.Add(new(Encoding.UTF8.GetString(data)));
        }

        return Task.FromResult<IReadOnlyList<QueuePeekMessage>>(results);
    }

    public Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default)
    {
        if (!_queues.TryGetValue(queueName, out var queue))
            _queues.TryAdd(queueName, queue = new());

        _ = ProcessQueueAsync(queueName, queue, onMessage, ct);
        return Task.FromResult(true);
    }

    public Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> DeleteExchange(string exchangeName, bool ifUnused = false, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data) => Task.FromResult(true);

    private async Task ProcessQueueAsync(string queueName, ConcurrentQueue<byte[]> queue, Func<byte[], Task<bool>> onMessage, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested) {
            if (!queue.TryDequeue(out var data)) {
                await Task.Delay(10, ct).ConfigureAwait(false);
                continue;
            }

            var requeue = await onMessage(data).ConfigureAwait(false);
            if (requeue)
                queue.Enqueue(data);
        }
    }
}