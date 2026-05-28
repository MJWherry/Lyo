using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Audit;

namespace Lyo.Authentication.Tests;

public class AuthAuditEventTests
{
    [Fact]
    public async Task RecordAsync_EnrichesFromContextAccessor()
    {
        var recorder = new CapturingRecorder();
        var context = new FakeContextAccessor("203.0.113.5", "ua/test", "trace-abc");
        await recorder.RecordAsync(context, null, AuthAuditEventKind.JwtIssued, Guid.NewGuid(), "jti", "google", "success", ct: TestContext.Current.CancellationToken);
        var evt = Assert.Single(recorder.Events);
        Assert.Equal(AuthAuditEventKind.JwtIssued, evt.Kind);
        Assert.Equal("203.0.113.5", evt.IpAddress);
        Assert.Equal("ua/test", evt.UserAgent);
        Assert.Equal("trace-abc", evt.CorrelationId);
        Assert.Equal("google", evt.Provider);
        Assert.Equal("success", evt.Outcome);
        Assert.NotEqual(Guid.Empty, evt.Id);
        Assert.True(evt.Timestamp > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task RecordAsync_NullContext_DoesNotThrow()
    {
        var recorder = new CapturingRecorder();
        await recorder.RecordAsync(null, null, AuthAuditEventKind.SignedOut, outcome: "success");
        var evt = Assert.Single(recorder.Events);
        Assert.Null(evt.IpAddress);
        Assert.Null(evt.UserAgent);
        Assert.Null(evt.CorrelationId);
    }

    [Fact]
    public async Task RecordAsync_ThrowingRecorder_IsSwallowed()
    {
        var recorder = new ThrowingRecorder();
        var ex = await Record.ExceptionAsync(() => recorder.RecordAsync(
            null, null, AuthAuditEventKind.TokenRejected, reason: "Revoked", ct: TestContext.Current.CancellationToken));

        Assert.Null(ex);
    }

    [Fact]
    public async Task NullAuthAuditRecorder_IsNoOp()
    {
        var evt = new AuthAuditEvent(Guid.NewGuid(), DateTime.UtcNow, AuthAuditEventKind.HandoffCodeConsumed);
        await NullAuthAuditRecorder.Instance.RecordAsync(evt, CancellationToken.None);
    }

    [Fact]
    public void NullAuthAuditContextAccessor_ReturnsNulls()
    {
        Assert.Null(NullAuthAuditContextAccessor.Instance.IpAddress);
        Assert.Null(NullAuthAuditContextAccessor.Instance.UserAgent);
        Assert.Null(NullAuthAuditContextAccessor.Instance.CorrelationId);
    }

    [Fact]
    public async Task RecordAsync_PropagatesTenantId()
    {
        var recorder = new CapturingRecorder();
        var tenant = Guid.NewGuid();
        await recorder.RecordAsync(null, null, AuthAuditEventKind.JwtIssued, tenantId: tenant, ct: TestContext.Current.CancellationToken);
        var evt = Assert.Single(recorder.Events);
        Assert.Equal(tenant, evt.TenantId);
    }

    [Fact]
    public async Task RecordAsync_PropagatesMetadata()
    {
        var recorder = new CapturingRecorder();
        var metadata = new Dictionary<string, object?> { ["jti"] = "abc", ["exp"] = 1234, ["nullable"] = null };
        await recorder.RecordAsync(null, null, AuthAuditEventKind.JwtIssued, metadata: metadata, ct: TestContext.Current.CancellationToken);
        var evt = Assert.Single(recorder.Events);
        Assert.NotNull(evt.Metadata);
        Assert.Equal(3, evt.Metadata!.Count);
        Assert.Equal("abc", evt.Metadata["jti"]);
        Assert.Equal(1234, evt.Metadata["exp"]);
    }

    private sealed class CapturingRecorder : IAuthAuditRecorder
    {
        public List<AuthAuditEvent> Events { get; } = [];

        public Task RecordAsync(AuthAuditEvent evt, CancellationToken ct = default)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingRecorder : IAuthAuditRecorder
    {
        public Task RecordAsync(AuthAuditEvent evt, CancellationToken ct = default) => throw new InvalidOperationException("boom");
    }

    private sealed class FakeContextAccessor(string? ip, string? ua, string? corr) : IAuthAuditContextAccessor
    {
        public string? IpAddress { get; } = ip;

        public string? UserAgent { get; } = ua;

        public string? CorrelationId { get; } = corr;
    }
}