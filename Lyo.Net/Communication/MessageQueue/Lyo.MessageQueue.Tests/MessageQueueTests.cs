using System.Text;
using System.Text.Json;
using Lyo.Result;
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
        await service.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(service.IsConnected());
        await service.DisconnectAsync(TestContext.Current.CancellationToken);
        Assert.False(service.IsConnected());
    }

    [Fact]
    public async Task InMemoryMqService_SendToQueue_and_SubscribeToQueue_delivers_messages()
    {
        var received = new List<byte[]>();
        using var service = new InMemoryMqService();
        await service.ConnectAsync(TestContext.Current.CancellationToken);
        await service.CreateQueue("test-queue", ct: TestContext.Current.CancellationToken);
        await service.SendToQueue("test-queue", Encoding.UTF8.GetBytes("message-1"));
        await service.SendToQueue("test-queue", Encoding.UTF8.GetBytes("message-2"));
        var cts = new CancellationTokenSource();
        var receivedCount = 0;
        _ = service.SubscribeToQueue(
            "test-queue", async data => {
                received.Add(data);
                receivedCount++;
                if (receivedCount >= 2)
                    cts.Cancel();

                return false;
            }, cts.Token);

        await Task.Delay(500, cts.Token).ContinueWith(_ => { }, TestContext.Current.CancellationToken);
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
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("worker-test", ct: TestContext.Current.CancellationToken);
        var request1 = new { Id = "a1", Payload = "first" };
        var request2 = new { Id = "a2", Payload = "second" };
        await mq.SendToQueue("worker-test", JsonSerializer.SerializeToUtf8Bytes(request1));
        await mq.SendToQueue("worker-test", JsonSerializer.SerializeToUtf8Bytes(request2));
        using var worker = new TestQueueWorker(mq, "worker-test");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token);
        for (var i = 0; i < 50 && worker.ProcessedRequests.Count < 2; i++)
            await Task.Delay(50, cts.Token);

        await worker.StopAsync(TestContext.Current.CancellationToken);
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
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("requeue-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("requeue-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}"));
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        _ = mq.SubscribeToQueue(
            "requeue-test", async _ => {
                callCount++;
                return callCount < 2;
            }, cts.Token);

        await Task.Delay(500, cts.Token);
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
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("envelope-test", ct: TestContext.Current.CancellationToken);
        var envelope = new QueueMessageEnvelope<TestRequest>(new("e1", "envelope-payload"), 0, "msg-1");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        await mq.SendToQueue("envelope-test", JsonSerializer.SerializeToUtf8Bytes(envelope, options));
        using var worker = new TestQueueWorker(mq, "envelope-test");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token);
        for (var i = 0; i < 50 && worker.ProcessedRequests.Count < 1; i++)
            await Task.Delay(50, cts.Token);

        await worker.StopAsync(TestContext.Current.CancellationToken);
        Assert.Single(worker.ProcessedRequests);
        Assert.Equal("e1", worker.ProcessedRequests[0].Id);
        Assert.Equal("envelope-payload", worker.ProcessedRequests[0].Payload);
    }

    [Fact]
    public async Task QueueWorkerBase_autocorrects_envelope_with_malformed_metadata()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("autocorrect-test", ct: TestContext.Current.CancellationToken);

        // EnqueuedAt is not a valid DateTime, so the full envelope deserialize throws — the Payload element alone is still recoverable.
        const string json = """{"Payload":{"Id":"a1","Payload":"autocorrected"},"RequeueCount":2,"MessageId":"m-1","EnqueuedAt":"not-a-date"}""";
        await mq.SendToQueue("autocorrect-test", Encoding.UTF8.GetBytes(json));
        using var worker = new TestQueueWorker(mq, "autocorrect-test");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token);
        for (var i = 0; i < 50 && worker.ProcessedRequests.Count < 1; i++)
            await Task.Delay(50, cts.Token);

        await worker.StopAsync(TestContext.Current.CancellationToken);
        Assert.Single(worker.ProcessedRequests);
        Assert.Equal("a1", worker.ProcessedRequests[0].Id);
        Assert.Equal("autocorrected", worker.ProcessedRequests[0].Payload);
    }

    [Fact]
    public async Task QueueWorkerBase_garbage_json_routed_to_dlq_not_redelivered()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("poison-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("poison-test", Encoding.UTF8.GetBytes("this is not json"));
        using var worker = new ConfigurableTestQueueWorker(mq, "poison-test", (r, _) => Result<TestRequest>.Success(r), dlqName: "poison-test.dlq");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StartAsync(cts.Token);
        IReadOnlyList<QueuePeekMessage> dlq = [];
        for (var i = 0; i < 50 && dlq.Count < 1; i++) {
            await Task.Delay(50, cts.Token);
            dlq = await mq.PeekQueueMessages("poison-test.dlq", ct: cts.Token);
        }

        await worker.StopAsync(TestContext.Current.CancellationToken);
        Assert.Single(dlq);
        Assert.Equal("this is not json", dlq[0].Payload);
        Assert.Equal(0, worker.CallCount);
    }

    [Fact]
    public async Task QueueWorkerBase_exception_retries_capped_then_dlq()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("throw-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("throw-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}"));
        using var worker = new ConfigurableTestQueueWorker(
            mq, "throw-test", (_, _) => throw new InvalidOperationException("boom"), 3, "throw-test.dlq");

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StartAsync(cts.Token);
        IReadOnlyList<QueuePeekMessage> dlq = [];
        for (var i = 0; i < 50 && dlq.Count < 1; i++) {
            await Task.Delay(50, cts.Token);
            dlq = await mq.PeekQueueMessages("throw-test.dlq", ct: cts.Token);
        }

        await worker.StopAsync(TestContext.Current.CancellationToken);

        // Initial attempt + 3 counted requeues, then DLQ — never an infinite broker loop.
        Assert.Equal(4, worker.CallCount);
        Assert.Single(dlq);
    }

    [Fact]
    public async Task QueueWorkerBase_failure_result_retries_capped_then_dlq()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("fail-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("fail-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}"));
        using var worker = new ConfigurableTestQueueWorker(
            mq, "fail-test", (_, _) => Result<TestRequest>.Failure("nope", "TestFailure"), 3, "fail-test.dlq");

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StartAsync(cts.Token);
        IReadOnlyList<QueuePeekMessage> dlq = [];
        for (var i = 0; i < 50 && dlq.Count < 1; i++) {
            await Task.Delay(50, cts.Token);
            dlq = await mq.PeekQueueMessages("fail-test.dlq", ct: cts.Token);
        }

        await worker.StopAsync(TestContext.Current.CancellationToken);
        Assert.Equal(4, worker.CallCount);
        Assert.Single(dlq);
    }

    [Fact]
    public async Task QueueWorkerBase_default_max_requeue_from_options()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("options-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("options-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}"));
        using var worker = new ConfigurableTestQueueWorker(
            mq, "options-test", (_, _) => Result<TestRequest>.Failure("nope", "TestFailure"), null, "options-test.dlq",
            new QueueWorkerOptions { DefaultMaxRequeueCount = 2 });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StartAsync(cts.Token);
        IReadOnlyList<QueuePeekMessage> dlq = [];
        for (var i = 0; i < 50 && dlq.Count < 1; i++) {
            await Task.Delay(50, cts.Token);
            dlq = await mq.PeekQueueMessages("options-test.dlq", ct: cts.Token);
        }

        await worker.StopAsync(TestContext.Current.CancellationToken);

        // Initial attempt + 2 counted requeues from QueueWorkerOptions.DefaultMaxRequeueCount.
        Assert.Equal(3, worker.CallCount);
        Assert.Single(dlq);
    }
}