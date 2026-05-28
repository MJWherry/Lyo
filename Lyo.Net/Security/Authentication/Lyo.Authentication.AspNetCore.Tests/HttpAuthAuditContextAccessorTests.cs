using System.Net;
using Lyo.Authentication.AspNetCore.Audit;
using Lyo.Diagnostic.Correlation;

namespace Lyo.Authentication.AspNetCore.Tests;

public sealed class HttpAuthAuditContextAccessorTests
{
    [Fact]
    public void CorrelationId_DelegatesToInjectedResolver()
    {
        var resolver = new StubResolver("resolved-id");
        var accessor = new HttpAuthAuditContextAccessor(new HttpContextAccessor(), resolver);
        Assert.Equal("resolved-id", accessor.CorrelationId);
        Assert.Equal(1, resolver.CallCount);
    }

    [Fact]
    public void CorrelationId_ReturnsNull_WhenResolverReturnsEmpty()
    {
        var resolver = new StubResolver("");
        var accessor = new HttpAuthAuditContextAccessor(new HttpContextAccessor(), resolver);
        Assert.Null(accessor.CorrelationId);
    }

    [Fact]
    public void IpAndUserAgent_SourcedFromHttpContext()
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Parse("203.0.113.5") } } };
        httpContextAccessor.HttpContext!.Request.Headers.UserAgent = "ua/test";
        var accessor = new HttpAuthAuditContextAccessor(httpContextAccessor, new StubResolver("ignored"));
        Assert.Equal("203.0.113.5", accessor.IpAddress);
        Assert.Equal("ua/test", accessor.UserAgent);
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
}