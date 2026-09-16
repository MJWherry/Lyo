using System.Net;
using Lyo.Exceptions.Models;
using Lyo.Http.Client.Pipeline;
using Lyo.RateLimit;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpRateLimitHandlerTests
{
    [Fact]
    public async Task SendAsync_On429_HonorsRetryAfter_ThenSucceeds()
    {
        var inner = new SequenceHandler();
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 10, Window = TimeSpan.FromSeconds(1), HonorRetryAfter = true, MaxRetriesOn429 = 2, Jitter = 0 };
        using var handler = Handler(options, new AllowingLimiter(), inner);
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("https://example.test/x", TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Sends);
    }

    [Fact]
    public void JitterWait_SameSeed_IsStable()
    {
        var options = new LyoHttpRateLimitOptions { Jitter = 0.2 };
        using var handler = Handler(options, null, new SequenceHandler());
        var wait = TimeSpan.FromSeconds(1);
        var a = handler.JitterWait(wait, new Random(42));
        var b = handler.JitterWait(wait, new Random(42));
        Assert.Equal(a, b);
        Assert.NotEqual(wait, a);
    }

    [Fact]
    public async Task SendAsync_RetryAfterAboveCap_WaitsCap()
    {
        var inner = new SequenceHandler(TimeSpan.FromHours(1));
        var options = new LyoHttpRateLimitOptions {
            Enabled = true,
            PermitLimit = 10,
            Window = TimeSpan.FromSeconds(1),
            HonorRetryAfter = true,
            MaxRetriesOn429 = 2,
            Jitter = 0.2,
            RetryAfterMaxWait = TimeSpan.FromMilliseconds(40)
        };
        using var handler = Handler(options, new AllowingLimiter(), inner);
        using var client = new HttpClient(handler);
        var started = DateTime.UtcNow;
        using var response = await client.GetAsync("https://example.test/slow", TestCt);
        var elapsed = DateTime.UtcNow - started;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Sends);
        Assert.True(elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SendAsync_EnabledFalse_DoesNotConsumeQuota()
    {
        var limiter = new CountingLimiter();
        var options = new LyoHttpRateLimitOptions { Enabled = false, Whitelist = ["GET /health"] };
        using var handler = Handler(options, limiter, new OkHandler());
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("https://example.test/health", TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(limiter.Calls);
    }

    [Fact]
    public async Task SendAsync_QueueFull_ThrowsBeforeSend()
    {
        var inner = new HoldHandler();
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 10, Window = TimeSpan.FromSeconds(1), QueueLimit = 1, Jitter = 0 };
        var gate = new LyoHttpRateLimitGate(options);
        using var handler = Handler(options, new AllowingLimiter(), inner, gate);
        using var client = new HttpClient(handler, disposeHandler: false);
        var first = client.GetAsync("https://example.test/hold", TestCt);
        await inner.Entered.Task.WaitAsync(TestCt);
        var ex = await Assert.ThrowsAsync<RateLimitExceededException>(() => client.GetAsync("https://example.test/hold", TestCt));
        Assert.Contains("queue", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, inner.Sends);
        inner.Release.TrySetResult();
        using var done = await first.WaitAsync(TestCt);
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
    }

    [Fact]
    public async Task SendAsync_EmptyBucket_WaitsThenSends()
    {
        var limiter = new CountingLimiter();
        limiter.Decisions.Enqueue(new RateLimitDecision(false, TimeSpan.FromMilliseconds(30), 1, 0));
        limiter.Decisions.Enqueue(new RateLimitDecision(true, null, 1, 0));
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 1, Window = TimeSpan.FromSeconds(2), QueueLimit = 4 };
        var inner = new OkHandler();
        using var handler = Handler(options, limiter, inner);
        using var client = new HttpClient(handler);
        var started = DateTime.UtcNow;
        using var response = await client.GetAsync("https://example.test/x", TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, inner.Sends);
        Assert.Equal(2, limiter.Calls.Count);
        Assert.True(DateTime.UtcNow - started >= TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public async Task SendAsync_OtherOptionsType_DoesNotShareQuota()
    {
        var limiter = new PerPolicyLimiter(TimeSpan.FromSeconds(30));
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 1, Window = TimeSpan.FromSeconds(10), QueueLimit = 4 };
        var innerA = new OkHandler();
        var innerB = new OkHandler();
        using var handlerA = Handler(options, limiter, innerA, policy: "Lyo.A.Options");
        using var handlerB = Handler(options, limiter, innerB, policy: "Lyo.B.Options");
        using var clientA = new HttpClient(handlerA);
        using var clientB = new HttpClient(handlerB);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestCt);
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        using var a = await clientA.GetAsync("https://example.test/x", cts.Token);
        using var b = await clientB.GetAsync("https://example.test/x", cts.Token);
        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        Assert.Equal(HttpStatusCode.OK, b.StatusCode);
    }

    [Fact]
    public async Task SendAsync_PartitionByHostFalse_SharesDefault()
    {
        var limiter = new CountingLimiter();
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 10, Window = TimeSpan.FromSeconds(1), PartitionByHost = false };
        using var handler = Handler(options, limiter, new OkHandler());
        using var client = new HttpClient(handler);
        using var first = await client.GetAsync("https://one.test/x", TestCt);
        using var second = await client.GetAsync("https://two.test/y", TestCt);
        Assert.Equal("default", limiter.Calls[0].PartitionKey);
        Assert.Equal("default", limiter.Calls[1].PartitionKey);
        Assert.Equal(RateLimitWindow.TokenBucket, limiter.Calls[0].Pattern);
    }

    [Fact]
    public void SendAsync_EnabledWithoutStore_Throws()
    {
        var services = new ServiceCollection();
        services.AddLyoHttpClient(o => {
            o.RateLimit.Enabled = true;
            o.RateLimit.PermitLimit = 1;
            o.RateLimit.Window = TimeSpan.FromSeconds(1);
            o.EnsureStatusCode = false;
        });
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient(nameof(ILyoHttpClient)));
        Assert.Contains("IRateLimitService", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_MaxConcurrent_ThirdCallDoesNotSend()
    {
        var inner = new HoldHandler();
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 10, Window = TimeSpan.FromSeconds(1), MaxConcurrent = 1, QueueLimit = 8 };
        var gate = new LyoHttpRateLimitGate(options);
        using var handler = Handler(options, new AllowingLimiter(), inner, gate);
        using var client = new HttpClient(handler, disposeHandler: false);
        var calls = new[] {
            client.GetAsync("https://example.test/1", TestCt),
            client.GetAsync("https://example.test/2", TestCt),
            client.GetAsync("https://example.test/3", TestCt)
        };
        await inner.Entered.Task.WaitAsync(TestCt);
        await Task.Delay(50, TestCt);
        Assert.Equal(1, inner.Sends);
        inner.Release.TrySetResult();
        await Task.WhenAll(calls);
    }

    [Fact]
    public async Task SendAsync_WhitelistedPath_DoesNotConsumeQuota()
    {
        var limiter = new CountingLimiter();
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 1, Window = TimeSpan.FromSeconds(10), Whitelist = ["GET /health"] };
        using var handler = Handler(options, limiter, new OkHandler());
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("https://example.test/health", TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(limiter.Calls);
    }

    [Fact]
    public async Task SendAsync_UnlistedPath_StillConsumesQuota()
    {
        var limiter = new CountingLimiter();
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 1, Window = TimeSpan.FromSeconds(10), Whitelist = ["GET /health"] };
        using var handler = Handler(options, limiter, new OkHandler());
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("https://example.test/search", TestCt);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(limiter.Calls);
    }

    [Fact]
    public void UserAgentResolve_SameSeed_PicksSameAgent()
    {
        var options = new LyoHttpUserAgentOptions {
            Enabled = true,
            Rotation = LyoHttpUserAgentRotation.PerSession,
            Agents = ["alpha", "beta"]
        };
        Assert.Equal(options.Resolve(7), options.Resolve(7));
        Assert.NotEqual(options.Resolve(0), options.Resolve(1));
    }

    private static LyoHttpRateLimitHandler Handler(
        LyoHttpRateLimitOptions options,
        IRateLimitService? limiter,
        HttpMessageHandler inner,
        LyoHttpRateLimitGate? gate = null,
        string policy = "Lyo.Http.Client.LyoHttpClientOptions")
    {
        var handler = new LyoHttpRateLimitHandler(options, limiter, policy, gate ?? new LyoHttpRateLimitGate(options)) { InnerHandler = inner };
        return handler;
    }

    private sealed class AllowingLimiter : IRateLimitService
    {
        public ValueTask<RateLimitDecision> TryAcquireAsync(RateLimitRequest request, CancellationToken ct = default)
            => new(new RateLimitDecision(true, null, request.PermitLimit, request.PermitLimit - 1));
    }

    private sealed class CountingLimiter : IRateLimitService
    {
        public List<RateLimitRequest> Calls { get; } = [];
        public Queue<RateLimitDecision> Decisions { get; } = new();

        public ValueTask<RateLimitDecision> TryAcquireAsync(RateLimitRequest request, CancellationToken ct = default)
        {
            Calls.Add(request);
            if (Decisions.Count > 0)
                return new(Decisions.Dequeue());

            return new(new RateLimitDecision(true, null, request.PermitLimit, 0));
        }
    }

    private sealed class PerPolicyLimiter(TimeSpan retry) : IRateLimitService
    {
        private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

        public ValueTask<RateLimitDecision> TryAcquireAsync(RateLimitRequest request, CancellationToken ct = default)
        {
            _counts.TryGetValue(request.Policy, out var count);
            count++;
            _counts[request.Policy] = count;
            if (count > request.PermitLimit)
                return new(new RateLimitDecision(false, retry, request.PermitLimit, 0));

            return new(new RateLimitDecision(true, null, request.PermitLimit, 0));
        }
    }

    private sealed class SequenceHandler(TimeSpan? retryAfter = null) : HttpMessageHandler
    {
        private readonly TimeSpan _retryAfter = retryAfter ?? TimeSpan.FromMilliseconds(10);
        public int Sends { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Sends++;
            if (Sends == 1) {
                var tooMany = new HttpResponseMessage((HttpStatusCode)429);
                tooMany.Headers.RetryAfter = new(_retryAfter);
                return Task.FromResult(tooMany);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
        }
    }

    private sealed class OkHandler : HttpMessageHandler
    {
        public int Sends { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Sends++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
        }
    }

    private sealed class HoldHandler : HttpMessageHandler
    {
        private int _sends;
        public int Sends => _sends;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var n = Interlocked.Increment(ref _sends);
            if (n == 1)
                Entered.TrySetResult();

            await Release.Task.WaitAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
