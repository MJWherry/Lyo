using System.Text;
using System.Text.Json;
using Lyo.Result;

namespace Lyo.MessageQueue.Tests;

public sealed class QueueWorkerBaseTests
{
    [Fact]
    public void Constructor_NullMqService_Throws() => Assert.Throws<ArgumentNullException>(() => new TestQueueWorker(null!, "queue"));

    [Fact]
    public void Constructor_NullQueueName_Throws()
    {
        using var mq = new InMemoryMqService();
        Assert.Throws<ArgumentNullException>(() => new TestQueueWorker(mq, null!));
    }

    [Fact]
    public void Constructor_EmptyQueueName_Throws()
    {
        using var mq = new InMemoryMqService();
        Assert.Throws<ArgumentException>(() => new TestQueueWorker(mq, ""));
    }

    [Fact]
    public async Task StartAsync_QueuedMessages_ProcessesThem()
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
    public void StartAsync_WhenDisposed_Throws()
    {
        using var mq = new InMemoryMqService();
        var worker = new TestQueueWorker(mq, "q");
        worker.Dispose();
        Assert.Throws<ObjectDisposedException>(() => worker.StartAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void IsRunning_BeforeStart_IsFalse()
    {
        using var mq = new InMemoryMqService();
        using var worker = new TestQueueWorker(mq, "q");
        Assert.False(worker.IsRunning);
    }

    [Fact]
    public async Task StartAsync_EnvelopeMessage_ProcessesPayload()
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
    public async Task StartAsync_MalformedEnvelopeMetadata_AutocorrectsPayload()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("autocorrect-test", ct: TestContext.Current.CancellationToken);

        // EnqueuedAt is not a valid DateTime, so whole-envelope deserialize fails — the Payload element can still be recovered.
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
    public async Task Handle_GarbageJson_RoutesToDlq()
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
    public async Task Handle_ThrownException_RetriesThenDlq()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("throw-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("throw-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}"));
        using var worker = new ConfigurableTestQueueWorker(mq, "throw-test", (_, _) => throw new InvalidOperationException("boom"), 3, "throw-test.dlq");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StartAsync(cts.Token);
        IReadOnlyList<QueuePeekMessage> dlq = [];
        for (var i = 0; i < 50 && dlq.Count < 1; i++) {
            await Task.Delay(50, cts.Token);
            dlq = await mq.PeekQueueMessages("throw-test.dlq", ct: cts.Token);
        }

        await worker.StopAsync(TestContext.Current.CancellationToken);

        // First try plus three counted requeues, then DLQ — the broker must not loop forever.
        Assert.Equal(4, worker.CallCount);
        Assert.Single(dlq);
    }

    [Fact]
    public async Task Handle_FailureResult_RetriesThenDlq()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("fail-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("fail-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}"));
        using var worker = new ConfigurableTestQueueWorker(mq, "fail-test", (_, _) => Result<TestRequest>.Failure("nope", "TestFailure"), 3, "fail-test.dlq");
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
    public async Task Handle_FailureResult_UsesOptionsMaxRequeue()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("options-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("options-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}"));
        using var worker = new ConfigurableTestQueueWorker(
            mq, "options-test", (_, _) => Result<TestRequest>.Failure("nope", "TestFailure"), null, "options-test.dlq", new() { DefaultMaxRequeueCount = 2, RequeueDelay = null });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StartAsync(cts.Token);
        IReadOnlyList<QueuePeekMessage> dlq = [];
        for (var i = 0; i < 50 && dlq.Count < 1; i++) {
            await Task.Delay(50, cts.Token);
            dlq = await mq.PeekQueueMessages("options-test.dlq", ct: cts.Token);
        }

        await worker.StopAsync(TestContext.Current.CancellationToken);

        // First try plus two counted requeues from QueueWorkerOptions.DefaultMaxRequeueCount.
        Assert.Equal(3, worker.CallCount);
        Assert.Single(dlq);
    }

    [Fact]
    public async Task Handle_FailureResult_SpacesRetriesByDelay()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestContext.Current.CancellationToken);
        await mq.CreateQueue("delay-test", ct: TestContext.Current.CancellationToken);
        await mq.SendToQueue("delay-test", Encoding.UTF8.GetBytes("{\"Id\":\"x\",\"Payload\":\"y\"}"));
        var attemptTimes = new List<DateTime>();
        using var worker = new ConfigurableTestQueueWorker(
            mq, "delay-test", (_, _) => {
                attemptTimes.Add(DateTime.UtcNow);
                return Result<TestRequest>.Failure("nope", "TestFailure");
            }, 2, "delay-test.dlq", new() { RequeueDelay = TimeSpan.FromMilliseconds(300) });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await worker.StartAsync(cts.Token);
        for (var i = 0; i < 100 && worker.CallCount < 3; i++)
            await Task.Delay(50, cts.Token);

        await worker.StopAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, attemptTimes.Count);

        // Linear backoff: attempt 2 waits about 1x the base delay, attempt 3 about 2x. Allow scheduling slack but require a real gap.
        Assert.True(attemptTimes[1] - attemptTimes[0] >= TimeSpan.FromMilliseconds(200), $"first retry gap too small: {attemptTimes[1] - attemptTimes[0]}");
        Assert.True(attemptTimes[2] - attemptTimes[1] >= TimeSpan.FromMilliseconds(400), $"second retry gap too small: {attemptTimes[2] - attemptTimes[1]}");
    }
}
