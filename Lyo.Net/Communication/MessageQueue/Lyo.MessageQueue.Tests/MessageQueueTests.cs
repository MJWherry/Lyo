using System.Text;
using System.Text.Json;
using Xunit;

namespace Lyo.MessageQueue.Tests;

public sealed class MessageQueueTests
{
    [Fact]
    public void IMqService_assembly_loads() => Assert.NotNull(typeof(IMqService));

    [Fact]
    public async Task InMemoryMqService_ConnectAsync_sets_IsConnected()
    {
        using var service = new InMemoryMqService();
        Assert.False(service.IsConnected());
        await service.ConnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(service.IsConnected());
        await service.DisconnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.False(service.IsConnected());
    }

    [Fact]
    public async Task InMemoryMqService_SendToQueue_and_SubscribeToQueue_delivers_messages()
    {
        var received = new List<byte[]>();
        using var service = new InMemoryMqService();
        await service.ConnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        await service.CreateQueue("test-queue", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await service.SendToQueue("test-queue", Encoding.UTF8.GetBytes("message-1")).ConfigureAwait(false);
        await service.SendToQueue("test-queue", Encoding.UTF8.GetBytes("message-2")).ConfigureAwait(false);
        var cts = new CancellationTokenSource();
        var receivedCount = 0;
        _ = service.SubscribeToQueue(
            "test-queue", async data => {
                received.Add(data);
                receivedCount++;
                if (receivedCount >= 2)
                    cts.Cancel();

                return false;
            }, cts.Token).ConfigureAwait(false);

        await Task.Delay(500, cts.Token).ContinueWith(_ => { }, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, received.Count);
        Assert.Equal("message-1", Encoding.UTF8.GetString(received[0]));
        Assert.Equal("message-2", Encoding.UTF8.GetString(received[1]));
    }

    [Fact]
    public void QueueWorkerBase_constructor_throws_on_null_mqService() => Assert.Throws<ArgumentNullException>(() => new TestQueueWorker(null!, "queue"));

    [Fact]
    public void QueueWorkerBase_constructor_throws_on_null_queueName()
    {
        using var mq = new InMemoryMqService();
        Assert.Throws<ArgumentNullException>(() => new TestQueueWorker(mq, null!));
    }

    [Fact]
    public void QueueWorkerBase_constructor_throws_on_empty_queueName()
    {
        using var mq = new InMemoryMqService();
        Assert.Throws<ArgumentException>(() => new TestQueueWorker(mq, ""));
    }

    [Fact]
    public async Task QueueWorkerBase_StartAsync_processes_messages()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        await mq.CreateQueue("worker-test", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var request1 = new { Id = "a1", Payload = "first" };
        var request2 = new { Id = "a2", Payload = "second" };
        await mq.SendToQueue("worker-test", JsonSerializer.SerializeToUtf8Bytes(request1)).ConfigureAwait(false);
        await mq.SendToQueue("worker-test", JsonSerializer.SerializeToUtf8Bytes(request2)).ConfigureAwait(false);
        using var worker = new TestQueueWorker(mq, "worker-test");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token).ConfigureAwait(false);
        for (var i = 0; i < 50 && worker.ProcessedRequests.Count < 2; i++)
            await Task.Delay(50, cts.Token).ConfigureAwait(false);

        worker.Stop();
        Assert.Equal(2, worker.ProcessedRequests.Count);
        Assert.Equal("a1", worker.ProcessedRequests[0].Id);
        Assert.Equal("first", worker.ProcessedRequests[0].Payload);
        Assert.Equal("a2", worker.ProcessedRequests[1].Id);
        Assert.Equal("second", worker.ProcessedRequests[1].Payload);
    }

    [Fact]
    public async Task QueueWorkerBase_requeues_on_failure()
    {
        var callCount = 0;
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        await mq.CreateQueue("requeue-test", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        await mq.SendToQueue("requeue-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}")).ConfigureAwait(false);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        _ = mq.SubscribeToQueue(
            "requeue-test", async _ => {
                callCount++;
                return callCount < 2;
            }, cts.Token).ConfigureAwait(false);

        await Task.Delay(500, cts.Token).ConfigureAwait(false);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void Disposed_QueueWorkerBase_StartAsync_throws()
    {
        using var mq = new InMemoryMqService();
        var worker = new TestQueueWorker(mq, "q");
        worker.Dispose();
        Assert.Throws<ObjectDisposedException>(() => worker.StartAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void QueueWorkerBase_IsRunning_reflects_state()
    {
        using var mq = new InMemoryMqService();
        using var worker = new TestQueueWorker(mq, "q");
        Assert.False(worker.IsRunning);
    }

    [Fact]
    public async Task QueueWorkerBase_processes_envelope_messages()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        await mq.CreateQueue("envelope-test", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        var envelope = new QueueMessageEnvelope<TestRequest>(new("e1", "envelope-payload"), 0, "msg-1");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        await mq.SendToQueue("envelope-test", JsonSerializer.SerializeToUtf8Bytes(envelope, options)).ConfigureAwait(false);
        using var worker = new TestQueueWorker(mq, "envelope-test");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token).ConfigureAwait(false);
        for (var i = 0; i < 50 && worker.ProcessedRequests.Count < 1; i++)
            await Task.Delay(50, cts.Token).ConfigureAwait(false);

        worker.Stop();
        Assert.Single(worker.ProcessedRequests);
        Assert.Equal("e1", worker.ProcessedRequests[0].Id);
        Assert.Equal("envelope-payload", worker.ProcessedRequests[0].Payload);
    }
}