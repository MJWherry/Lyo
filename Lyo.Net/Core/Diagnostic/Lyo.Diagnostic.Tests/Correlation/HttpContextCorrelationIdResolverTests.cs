using System.Diagnostics;
using Lyo.Diagnostic.AspNetCore;
using Lyo.Diagnostic.AspNetCore.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Lyo.Diagnostic.Tests.Correlation;

public sealed class HttpContextCorrelationIdResolverTests
{
    [Fact]
    public void Resolve_WalksConfiguredHeaders_InOrder()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Request-Id"] = "second-value";
        ctx.Request.Headers["X-Correlation-Id"] = "first-value";
        var resolver = Build(ctx, opt => opt.CorrelationIdHeaders = ["X-Correlation-Id", "X-Request-Id"]);
        Assert.Equal("first-value", resolver.Resolve());
    }

    [Fact]
    public void Resolve_PrefersAliasHeader_WhenPrimaryMissing()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Request-Id"] = "alias-value";
        var resolver = Build(ctx, opt => opt.CorrelationIdHeaders = ["X-Correlation-Id", "X-Request-Id"]);
        Assert.Equal("alias-value", resolver.Resolve());
    }

    [Fact]
    public void Resolve_FallsBackTo_TraceIdentifier_WhenHeadersMissing()
    {
        var ctx = new DefaultHttpContext { TraceIdentifier = "trace-abc" };
        var resolver = Build(ctx);
        Assert.Equal("trace-abc", resolver.Resolve());
    }

    [Fact]
    public void Resolve_UsesActivity_WhenNoHttpContext()
    {
        using var activity = new Activity("test").Start();
        var resolver = Build(null);
        var id = resolver.Resolve();
        Assert.Equal(activity.Id, id);
    }

    [Fact]
    public void Resolve_FallsBackToGuid_WhenNothingIsAvailable()
    {
        var current = Activity.Current;
        Activity.Current = null;
        try {
            var resolver = Build(null);
            var id = resolver.Resolve();
            Assert.False(string.IsNullOrWhiteSpace(id));
            Assert.True(Guid.TryParseExact(id, "N", out var _));
        }
        finally {
            Activity.Current = current;
        }
    }

    private static HttpContextCorrelationIdResolver Build(HttpContext? httpContext, Action<DiagnosticWebOptions>? configure = null)
    {
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var options = new DiagnosticWebOptions();
        configure?.Invoke(options);
        return new(accessor, Options.Create(options));
    }
}