using System.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions.Models;
using Lyo.Http.Client;
using Lyo.Http.Client.Pipeline;
using Lyo.Http.Client.Plan;

namespace Lyo.Http.Client.Tests;

public sealed class LyoHttpRateLimitHandlerTests
{
    [Fact]
    public async Task SendAsync_On429_HonorsRetryAfter_ThenSucceeds()
    {
        var inner = new SequenceHandler();
        var options = new LyoHttpRateLimitOptions { Enabled = true, PermitLimit = 10, Window = TimeSpan.FromSeconds(1), HonorRetryAfter = true, MaxRetriesOn429 = 2, Jitter = 0 };
        var limiter = new LyoHttpRateLimiter(options);
        using var handler = new LyoHttpRateLimitHandler(options, limiter) { InnerHandler = inner };
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("https://example.test/x", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Sends);
    }

    [Fact]
    public void JitterWait_SameSeed_IsStable()
    {
        var options = new LyoHttpRateLimitOptions { Jitter = 0.2 };
        var limiter = new LyoHttpRateLimiter(options);
        var wait = TimeSpan.FromSeconds(1);
        var a = limiter.JitterWait(wait, new Random(42));
        var b = limiter.JitterWait(wait, new Random(42));
        Assert.Equal(a, b);
        Assert.NotEqual(wait, a);
    }

    [Fact]
    public void UserAgentResolve_SameSeed_PicksSameAgent()
    {
        var options = new LyoHttpUserAgentOptions { Enabled = true, Rotation = LyoHttpUserAgentRotation.PerSession };
        Assert.Equal(options.Resolve(7), options.Resolve(7));
        Assert.NotEqual(options.Resolve(0), options.Resolve(1));
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        public int Sends { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Sends++;
            if (Sends == 1) {
                var tooMany = new HttpResponseMessage((HttpStatusCode)429);
                tooMany.Headers.RetryAfter = new(TimeSpan.FromMilliseconds(10));
                return Task.FromResult(tooMany);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
        }
    }
}

