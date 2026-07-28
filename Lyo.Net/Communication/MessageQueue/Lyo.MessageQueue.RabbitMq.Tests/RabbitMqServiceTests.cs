using System.Text;
using Xunit;

namespace Lyo.MessageQueue.RabbitMq.Tests;

/// <summary>Integration tests for <see cref="RabbitMqService" /> against a real RabbitMQ broker (Testcontainers). Requires Docker.</summary>
public sealed class RabbitMqServiceTests
{
    private readonly RabbitMqBrokerFixture _broker;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public RabbitMqServiceTests(RabbitMqBrokerFixture broker) => _broker = broker;

    private static string UniqueQueue(string prefix) => $"{prefix}.{Guid.NewGuid():N}";

    /// <summary>Polls until <paramref name="condition" /> returns true or the timeout elapses. Management API statistics lag a few seconds behind broker state.</summary>
    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < deadline) {
            if (await condition())
                return true;

            await Task.Delay(200, Ct);
        }

        return await condition();
    }

    [Fact]
    public async Task Connect_disconnect_reconnect_works_on_same_instance()
    {
        var service = _broker.CreateService();
        Assert.False(service.IsConnected());
        await service.ConnectAsync(Ct);
        Assert.True(service.IsConnected());
        await service.DisconnectAsync(Ct);
        Assert.False(service.IsConnected());

        // The regression this guards: DisconnectAsync used to mark the instance disposed, making reconnects impossible.
        await service.ConnectAsync(Ct);
        Assert.True(service.IsConnected());
        await service.DisposeAsync();
    }

    [Fact]
    public async Task Create_clear_delete_queue_and_peek_via_management_api()
    {
        await using var service = _broker.CreateService();
        await service.ConnectAsync(Ct);
        var queue = UniqueQueue("lifecycle");
        Assert.True(await service.CreateQueue(queue, ct: Ct));
        Assert.True(await service.SendToQueue(queue, Encoding.UTF8.GetBytes("peek-me")));

        // Peek does not ack, so the message stays on the queue.
        Assert.True(await WaitUntilAsync(async () => (await service.PeekQueueMessages(queue, ct: Ct)).Count == 1));
        var peeked = await service.PeekQueueMessages(queue, ct: Ct);
        Assert.Equal("peek-me", peeked[0].Payload);
        Assert.True(await service.ClearQueue(queue, Ct));
        Assert.True(await WaitUntilAsync(async () => (await service.PeekQueueMessages(queue, ct: Ct)).Count == 0));
        Assert.True(await service.DeleteQueue(queue, ct: Ct));
    }

    [Fact]
    public async Task SubscribeToQueue_fails_when_queue_does_not_exist()
    {
        await using var service = _broker.CreateService();
        await service.ConnectAsync(Ct);
        var queue = UniqueQueue("missing");
        Assert.False(await service.SubscribeToQueue(queue, _ => Task.FromResult(false), Ct));
    }

    [Fact]
    public async Task Publish_subscribe_roundtrip_delivers_persistent_message()
    {
        await using var service = _broker.CreateService();
        await service.ConnectAsync(Ct);
        var queue = UniqueQueue("roundtrip");
        await service.CreateQueue(queue, ct: Ct);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.True(
            await service.SubscribeToQueue(
                queue, bytes => {
                    received.TrySetResult(Encoding.UTF8.GetString(bytes));
                    return Task.FromResult(false);
                }, Ct));

        Assert.True(await service.SendToQueue(queue, Encoding.UTF8.GetBytes("hello-rabbit")));
        var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.Equal("hello-rabbit", payload);
    }

    [Fact]
    public async Task Handler_true_redelivers_false_acks()
    {
        await using var service = _broker.CreateService();
        await service.ConnectAsync(Ct);
        var queue = UniqueQueue("requeue");
        await service.CreateQueue(queue, ct: Ct);
        var deliveries = 0;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await service.SubscribeToQueue(
            queue, _ => {
                var count = Interlocked.Increment(ref deliveries);
                if (count >= 2) {
                    done.TrySetResult();
                    return Task.FromResult(false); // ack on the second delivery
                }

                return Task.FromResult(true); // requeue the first delivery
            }, Ct);

        await service.SendToQueue(queue, Encoding.UTF8.GetBytes("retry-me"));
        await done.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.Equal(2, deliveries);

        // After the ack, the queue drains to empty.
        Assert.True(await WaitUntilAsync(async () => (await service.PeekQueueMessages(queue, ct: Ct)).Count == 0));
    }

    [Fact]
    public async Task Per_queue_concurrency_limits_are_enforced()
    {
        var limitedQueue = UniqueQueue("limit1");
        var parallelQueue = UniqueQueue("limit4");
        await using var service = _broker.CreateService(o => o.QueueProcessingLimits = new() { [limitedQueue] = 1, [parallelQueue] = 4 });
        await service.ConnectAsync(Ct);
        await service.CreateQueue(limitedQueue, ct: Ct);
        await service.CreateQueue(parallelQueue, ct: Ct);
        const int messageCount = 8;
        var limitedTracker = new ParallelismTracker(messageCount);
        var parallelTracker = new ParallelismTracker(messageCount);
        await service.SubscribeToQueue(limitedQueue, _ => limitedTracker.TrackAsync(), Ct);
        await service.SubscribeToQueue(parallelQueue, _ => parallelTracker.TrackAsync(), Ct);
        for (var i = 0; i < messageCount; i++) {
            await service.SendToQueue(limitedQueue, Encoding.UTF8.GetBytes($"m{i}"));
            await service.SendToQueue(parallelQueue, Encoding.UTF8.GetBytes($"m{i}"));
        }

        await Task.WhenAll(limitedTracker.AllProcessed, parallelTracker.AllProcessed).WaitAsync(TimeSpan.FromSeconds(30), Ct);
        Assert.Equal(1, limitedTracker.MaxObservedParallelism);
        Assert.True(parallelTracker.MaxObservedParallelism > 1, $"expected parallel queue to exceed 1 concurrent message, observed {parallelTracker.MaxObservedParallelism}");
        Assert.True(parallelTracker.MaxObservedParallelism <= 4, $"expected parallel queue to stay at or below its limit of 4, observed {parallelTracker.MaxObservedParallelism}");
    }

    [Fact]
    public async Task Publisher_confirms_path_returns_true_on_success()
    {
        await using var service = _broker.CreateService(o => o.PublisherConfirms = true);
        await service.ConnectAsync(Ct);
        var queue = UniqueQueue("confirms");
        await service.CreateQueue(queue, ct: Ct);
        Assert.True(await service.SendToQueue(queue, Encoding.UTF8.GetBytes("confirmed")));
        Assert.True(await WaitUntilAsync(async () => (await service.PeekQueueMessages(queue, ct: Ct)).Count == 1));
    }

    [Fact]
    public async Task Delayed_message_arrives_after_not_before_its_delay()
    {
        await using var service = _broker.CreateService();
        await service.ConnectAsync(Ct);
        var queue = UniqueQueue("delayed");
        await service.CreateQueue(queue, ct: Ct);
        var received = new TaskCompletionSource<DateTime>(TaskCreationOptions.RunContinuationsAsynchronously);
        await service.SubscribeToQueue(
            queue, _ => {
                received.TrySetResult(DateTime.UtcNow);
                return Task.FromResult(false);
            }, Ct);

        var delay = TimeSpan.FromMilliseconds(1500);
        var sentAt = DateTime.UtcNow;
        Assert.True(await service.SendToQueueDelayed(queue, Encoding.UTF8.GetBytes("later"), delay, Ct));

        // Not before: nothing is delivered while the message sits in the wait queue.
        await Task.Delay(700, Ct);
        Assert.False(received.Task.IsCompleted, "message was delivered before its delay elapsed");
        var receivedAt = await received.Task.WaitAsync(TimeSpan.FromSeconds(15), Ct);
        Assert.True(receivedAt - sentAt >= TimeSpan.FromMilliseconds(1200), $"message arrived too early: {receivedAt - sentAt}");
    }

    [Fact]
    public async Task Dlq_auto_wiring_routes_broker_rejected_messages_to_dlq()
    {
        await using var service = _broker.CreateService();
        await service.ConnectAsync(Ct);
        var queue = UniqueQueue("dlqwired");
        var dlq = $"{queue}.dlq";

        // Per-queue TTL means an unconsumed message is dead-lettered by the broker itself — the application never touches it.
        Assert.True(await service.CreateQueueWithDlq(queue, arguments: new Dictionary<string, object> { ["x-message-ttl"] = 500 }, ct: Ct));
        Assert.True(await service.SendToQueue(queue, Encoding.UTF8.GetBytes("reject-me")));
        Assert.True(await WaitUntilAsync(async () => (await service.PeekQueueMessages(dlq, ct: Ct)).Count == 1), "expected the expired message to land in the DLQ");
        var dlqMessages = await service.PeekQueueMessages(dlq, ct: Ct);
        Assert.Equal("reject-me", dlqMessages[0].Payload);
    }

    [Fact]
    public async Task Queue_stats_reflect_published_messages()
    {
        await using var service = _broker.CreateService();
        await service.ConnectAsync(Ct);
        var queue = UniqueQueue("stats");
        await service.CreateQueue(queue, ct: Ct);
        for (var i = 0; i < 3; i++)
            await service.SendToQueue(queue, Encoding.UTF8.GetBytes($"s{i}"));

        // Management API statistics update on an emission interval — poll until they catch up.
        Assert.True(
            await WaitUntilAsync(
                async () => {
                    var info = await service.GetQueueInfoAsync(queue, Ct);
                    return info is { Messages: 3 };
                }, TimeSpan.FromSeconds(30)));

        var all = await service.GetAllQueuesInfoAsync(Ct);
        Assert.Contains(all, q => q.Name == queue);
        var missing = await service.GetQueueInfoAsync($"does-not-exist-{Guid.NewGuid():N}", Ct);
        Assert.Null(missing);
    }

    /// <summary>Tracks the maximum number of concurrently running handler invocations across a fixed number of messages.</summary>
    private sealed class ParallelismTracker
    {
        private readonly TaskCompletionSource _allProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly int _expectedCount;
        private int _current;
        private int _max;
        private int _processed;

        public Task AllProcessed => _allProcessed.Task;

        public int MaxObservedParallelism => _max;

        public ParallelismTracker(int expectedCount) => _expectedCount = expectedCount;

        public async Task<bool> TrackAsync()
        {
            var current = Interlocked.Increment(ref _current);
            InterlockedMax(ref _max, current);
            await Task.Delay(300);
            Interlocked.Decrement(ref _current);
            if (Interlocked.Increment(ref _processed) >= _expectedCount)
                _allProcessed.TrySetResult();

            return false;
        }

        private static void InterlockedMax(ref int location, int value)
        {
            int snapshot;
            do {
                snapshot = Volatile.Read(ref location);
                if (value <= snapshot)
                    return;
            } while (Interlocked.CompareExchange(ref location, value, snapshot) != snapshot);
        }
    }
}