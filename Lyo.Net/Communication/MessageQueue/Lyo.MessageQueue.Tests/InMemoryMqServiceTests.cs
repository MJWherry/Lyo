using System.Text;
using System.Text.Json;

namespace Lyo.MessageQueue.Tests;

public sealed class InMemoryMqServiceTests
{
    [Fact]
    public async Task ConnectAsync_WhenCalled_SetsIsConnected()
    {
        using var service = new InMemoryMqService();
        Assert.False(service.IsConnected());
        await service.ConnectAsync(TestCt);
        Assert.True(service.IsConnected());
        await service.DisconnectAsync(TestCt);
        Assert.False(service.IsConnected());
    }

    [Fact]
    public async Task SendToQueue_WithSubscriber_DeliversMessages()
    {
        var received = new List<byte[]>();
        using var service = new InMemoryMqService();
        await service.ConnectAsync(TestCt);
        await service.CreateQueue("test-queue", ct: TestCt);
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

        await Task.Delay(500, cts.Token).ContinueWith(_ => { }, TestCt);
        Assert.Equal(2, received.Count);
        Assert.Equal("message-1", Encoding.UTF8.GetString(received[0]));
        Assert.Equal("message-2", Encoding.UTF8.GetString(received[1]));
    }

    [Fact]
    public async Task SubscribeToQueue_OnFailure_Requeues()
    {
        var callCount = 0;
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestCt);
        await mq.CreateQueue("requeue-test", ct: TestCt);
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
    public async Task SubscribeToQueueAsync_EnvelopedArrives_BarePayloadDoesNot()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestCt);
        await mq.CreateQueue("typed-test", ct: TestCt);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        await mq.SendToQueueWithEnvelopeAsync("typed-test", new TestRequest("e1", "enveloped"), options, traceId: "trace-1");
        await mq.SendToQueue("typed-test", JsonSerializer.SerializeToUtf8Bytes(new TestRequest("l1", "bare"), options));
        var received = new List<(TestRequest Payload, QueueMessageEnvelope<TestRequest>? Envelope)>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        _ = mq.SubscribeToQueueAsync<TestRequest>(
            "typed-test", (payload, envelope) => {
                received.Add((payload, envelope));
                return Task.FromResult(false);
            }, options, cts.Token);

        for (var i = 0; i < 50 && received.Count < 1; i++)
            await Task.Delay(50, cts.Token);

        await Task.Delay(200, cts.Token);
        var only = Assert.Single(received);
        Assert.Equal("e1", only.Payload.Id);
        Assert.NotNull(only.Envelope);
        Assert.Equal("trace-1", only.Envelope!.TraceId);
    }

    [Fact]
    public async Task SubscribeToQueueAsync_UnparseableMessage_AcksInsteadOfLooping()
    {
        using var mq = new InMemoryMqService();
        await mq.ConnectAsync(TestCt);
        await mq.CreateQueue("typed-poison-test", ct: TestCt);
        await mq.SendToQueue("typed-poison-test", Encoding.UTF8.GetBytes("not json at all"));
        await mq.SendToQueueWithEnvelopeAsync("typed-poison-test", new TestRequest("ok", "good"), new());
        var received = new List<TestRequest>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        _ = mq.SubscribeToQueueAsync<TestRequest>(
            "typed-poison-test", (payload, _) => {
                received.Add(payload);
                return Task.FromResult(false);
            }, new() { PropertyNameCaseInsensitive = true }, cts.Token);

        for (var i = 0; i < 50 && received.Count < 1; i++)
            await Task.Delay(50, cts.Token);

        // The poison message is acked away — only the valid one hits the handler, and the queue empties instead of redelivering forever.
        await Task.Delay(200, cts.Token);
        Assert.Single(received);
        Assert.Equal("ok", received[0].Id);
        var remaining = await mq.PeekQueueMessages("typed-poison-test", ct: cts.Token);
        Assert.Empty(remaining);
    }
}