using System.Net;
using Lyo.Diagnostic.Correlation;

namespace Lyo.Diagnostic.Tests.Correlation;

public sealed class LyoCorrelationDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_StampsHeader_WhenMissing()
    {
        var resolver = new StubResolver("trace-1");
        var (handler, inner) = CreateHandler(resolver);
        using var client = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        await client.GetAsync("api/x", TestContext.Current.CancellationToken);
        Assert.True(inner.LastRequest!.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Equal("trace-1", Assert.Single(values));
    }

    [Fact]
    public async Task SendAsync_RespectsExistingHeader()
    {
        var resolver = new StubResolver("trace-1");
        var (handler, inner) = CreateHandler(resolver);
        using var client = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/x");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", "caller-supplied");
        await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.True(inner.LastRequest!.Headers.TryGetValues("X-Correlation-Id", out var values));
        Assert.Equal("caller-supplied", Assert.Single(values));
        Assert.Equal(0, resolver.CallCount);
    }

    [Fact]
    public async Task SendAsync_WritesAllConfiguredHeaderNames()
    {
        var resolver = new StubResolver("trace-1");
        var options = new CorrelationHandlerOptions { DetectHeaderNames = ["X-Correlation-Id", "X-Request-Id"], WriteHeaderNames = ["X-Correlation-Id", "X-Request-Id"] };
        var (handler, inner) = CreateHandler(resolver, options);
        using var client = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        await client.GetAsync("api/x", TestContext.Current.CancellationToken);
        Assert.True(inner.LastRequest!.Headers.TryGetValues("X-Correlation-Id", out var corr));
        Assert.True(inner.LastRequest.Headers.TryGetValues("X-Request-Id", out var req));
        Assert.Equal("trace-1", Assert.Single(corr));
        Assert.Equal("trace-1", Assert.Single(req));
    }

    [Fact]
    public async Task SendAsync_TreatsAnyDetectedHeader_AsAlreadyStamped()
    {
        var resolver = new StubResolver("trace-1");
        var options = new CorrelationHandlerOptions { DetectHeaderNames = ["X-Correlation-Id", "X-Request-Id"], WriteHeaderNames = ["X-Correlation-Id", "X-Request-Id"] };
        var (handler, inner) = CreateHandler(resolver, options);
        using var client = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/x");
        request.Headers.TryAddWithoutValidation("X-Request-Id", "alias-supplied");
        await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.True(inner.LastRequest!.Headers.TryGetValues("X-Request-Id", out var values));
        Assert.Equal("alias-supplied", Assert.Single(values));
        Assert.False(inner.LastRequest.Headers.Contains("X-Correlation-Id"));
        Assert.Equal(0, resolver.CallCount);
    }

    [Fact]
    public async Task SendAsync_DoesNothing_WhenResolverReturnsEmpty()
    {
        var resolver = new StubResolver("");
        var (handler, inner) = CreateHandler(resolver);
        using var client = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        await client.GetAsync("api/x", TestContext.Current.CancellationToken);
        Assert.False(inner.LastRequest!.Headers.Contains("X-Correlation-Id"));
    }

    private static (LyoCorrelationDelegatingHandler Handler, StubInnerHandler Inner) CreateHandler(StubResolver resolver, CorrelationHandlerOptions? options = null)
    {
        var inner = new StubInnerHandler();
        var handler = new LyoCorrelationDelegatingHandler(resolver, options) { InnerHandler = inner };
        return (handler, inner);
    }

    private sealed class StubResolver(string id) : ICorrelationIdResolver
    {
        public int CallCount { get; private set; }

        public string Resolve()
        {
            CallCount++;
            return id;
        }
    }

    private sealed class StubInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}