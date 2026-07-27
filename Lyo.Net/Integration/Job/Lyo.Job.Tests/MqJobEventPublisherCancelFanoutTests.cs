using System.Collections.Concurrent;
using Lyo.Health;
using Lyo.Job.Client;
using Lyo.Job.Models;
using Lyo.MessageQueue;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lyo.Job.Tests;

/// <summary>
/// Regression tests for the cancellation fanout fix: each worker instance must declare its own exclusive, auto-delete queue bound to the cancel routing key, so every
/// instance of a scaled-out worker type sees every cancel message. The old shared per-worker-type queue was a competing-consumer queue that silently dropped cancels.
/// </summary>
public class MqJobEventPublisherCancelFanoutTests
{
    [Fact]
    public async Task SubscribeToRunCancellations_DeclaresPerInstanceExclusiveQueueBoundToCancelKey()
    {
        var mq = new RoutingFakeMqService();
        var publisher = CreatePublisher(mq);

        await publisher.SubscribeToRunCancellationsAsync("cs", _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        var queue = Assert.Single(mq.CreatedQueues);
        Assert.StartsWith(Constants.Mq.QueueGetJobRunCancel("cs") + ".", queue.Name, StringComparison.Ordinal);
        Assert.True(queue.Exclusive);
        Assert.True(queue.AutoDelete);

        var binding = Assert.Single(mq.Bindings);
        Assert.Equal(queue.Name, binding.QueueName);
        Assert.Equal(Constants.Mq.JobEventExchange, binding.ExchangeName);
        Assert.Equal(Constants.Mq.JobRunCancelledRoutingKey, binding.RoutingKey);
    }

    [Fact]
    public async Task PublishRunCancelled_ReachesEverySubscribedInstance()
    {
        var mq = new RoutingFakeMqService();
        var publisher = CreatePublisher(mq);
        var receivedByInstanceA = new ConcurrentBag<Guid>();
        var receivedByInstanceB = new ConcurrentBag<Guid>();

        // Two instances of the same worker type — with the old shared queue only one of them would receive each cancel.
        await publisher.SubscribeToRunCancellationsAsync("cs", runId => {
            receivedByInstanceA.Add(runId);
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);
        await publisher.SubscribeToRunCancellationsAsync("cs", runId => {
            receivedByInstanceB.Add(runId);
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        Assert.Equal(2, mq.CreatedQueues.Count);
        Assert.Equal(2, mq.CreatedQueues.Select(q => q.Name).Distinct().Count());

        var runId = Guid.NewGuid();
        await publisher.PublishRunCancelledAsync(runId, TestContext.Current.CancellationToken);

        Assert.Equal(runId, Assert.Single(receivedByInstanceA));
        Assert.Equal(runId, Assert.Single(receivedByInstanceB));
    }

    private static MqJobEventPublisher CreatePublisher(IMqService mq)
        => new(mq, NullLogger<MqJobEventPublisher>.Instance, Options.Create(new JobMqOptions()));

    /// <summary>Fake MQ that records queue declarations/bindings and routes exchange publishes to every bound queue's subscriber (broadcast semantics).</summary>
    private sealed class RoutingFakeMqService : IMqService
    {
        private readonly ConcurrentDictionary<string, Func<byte[], Task<bool>>> _subscribers = new();

        public List<(string Name, bool Durable, bool Exclusive, bool AutoDelete)> CreatedQueues { get; } = [];

        public List<(string QueueName, string ExchangeName, string RoutingKey)> Bindings { get; } = [];

        public bool IsConnected() => true;

        public Task ConnectAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;

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

        public Task<bool> SendToQueue(string queueName, byte[] data)
            => _subscribers.TryGetValue(queueName, out var handler) ? handler(data).ContinueWith(_ => true) : Task.FromResult(true);

        public async Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data)
        {
            foreach (var binding in Bindings.Where(b => b.ExchangeName == exchangeName && b.RoutingKey == routingKey)) {
                if (_subscribers.TryGetValue(binding.QueueName, out var handler))
                    await handler(data);
            }

            return true;
        }

        public Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<QueuePeekMessage>>([]);

        public Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default)
        {
            _subscribers[queueName] = onMessage;
            return Task.FromResult(true);
        }

        public string HealthCheckName => "routing-fake-mq";

        public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
            => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero, null, new Dictionary<string, object?>()));
    }
}
