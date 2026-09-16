using Lyo.Audit;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.Authorization.Tests;

public sealed class GrantStoreTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExactGrant_Allows_MissingExpiredAndRevoked_Deny()
    {
        var store = Store();
        await store.SaveAsync(Write("user", "read", "note", "n1"), TestCt);
        Assert.True(await store.IsAllowedAsync(Check("user", "read", "note", "n1"), TestCt));
        Assert.False(await store.IsAllowedAsync(Check("user", "write", "note", "n1"), TestCt));

        var expiring = Store();
        await expiring.SaveAsync(Write("user", "read", "note", "n1", expiresAt: Now), TestCt);
        Assert.False(await expiring.IsAllowedAsync(Check("user", "read", "note", "n1"), TestCt));

        var revoked = Store();
        var saved = await revoked.SaveAsync(Write("user", "read", "note", "n1"), TestCt);
        await revoked.RevokeAsync(saved.Id, TestCt);
        Assert.False(await revoked.IsAllowedAsync(Check("user", "read", "note", "n1"), TestCt));
    }

    [Fact]
    public async Task Wildcards_MatchOnlyInTheDocumentedDirection()
    {
        var store = Store();
        await store.SaveAsync(Write("user", "read", "note", resourceId: null), TestCt);
        Assert.True(await store.IsAllowedAsync(Check("user", "read", "note", "n1"), TestCt));
        Assert.False(await store.IsAllowedAsync(Check("other", "read", "note", "n1"), TestCt));

        var specific = Store();
        await specific.SaveAsync(Write("user", "read", "note", "n1"), TestCt);
        Assert.False(await specific.IsAllowedAsync(Check("user", "read", "note", resourceId: null), TestCt));

        var anyTenant = Store();
        await anyTenant.SaveAsync(Write("user", "read", "note", "n1", tenant: null), TestCt);
        Assert.True(await anyTenant.IsAllowedAsync(Check("user", "read", "note", "n1", tenant: Guid.NewGuid()), TestCt));
        Assert.False(await anyTenant.IsAllowedAsync(Check("user", "read", "note", "other", tenant: Guid.NewGuid()), TestCt));

        var oneTenant = Guid.NewGuid();
        var tenantWide = Store();
        await tenantWide.SaveAsync(Write("user", "read", "note", resourceId: null, tenant: oneTenant), TestCt);
        Assert.True(await tenantWide.IsAllowedAsync(Check("user", "read", "note", "n1", oneTenant), TestCt));
        Assert.False(await tenantWide.IsAllowedAsync(Check("user", "read", "note", "n1", Guid.NewGuid()), TestCt));
        Assert.False(await tenantWide.IsAllowedAsync(Check("user", "read", "note", "n1", tenant: null), TestCt));
    }

    [Fact]
    public async Task Match_IsOrdinal_Trimmed_AndGuidIdsAreLowercase()
    {
        var store = Store();
        await store.SaveAsync(Write("user", "read", "note", "n1"), TestCt);
        Assert.False(await store.IsAllowedAsync(Check("user", "read", "Note", "n1"), TestCt));
        Assert.True(await store.IsAllowedAsync(Check(" user ", " read ", " note ", " n1 "), TestCt));

        var guid = Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
        var saved = await store.SaveAsync(Write(guid.ToString("D").ToUpperInvariant(), "read", "note", guid.ToString("D").ToUpperInvariant()), TestCt);
        Assert.Equal(guid.ToString("D").ToUpperInvariant(), saved.Subject);
        Assert.Equal(guid.ToString("D"), saved.ResourceId);
        Assert.True(await store.IsAllowedAsync(Check(saved.Subject, "read", "note", guid.ToString("D")), TestCt));

        var text = await store.SaveAsync(Write("user", "read", "note", "Not-A-Guid"), TestCt);
        Assert.Equal("Not-A-Guid", text.ResourceId);
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(Write("user", "read", "note", " "), TestCt));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(Write("user", "read", "note", expiresAt: DateTime.SpecifyKind(Now, DateTimeKind.Local)), TestCt));
    }

    [Fact]
    public async Task Duplicate_Conflicts_Supersede_Revoke_AndList()
    {
        var store = Store();
        await store.SaveAsync(Write("user", "read", "note", "n1"), TestCt);
        await Assert.ThrowsAsync<ConflictException>(() => store.SaveAsync(Write("user", "read", "note", "n1"), TestCt));

        var clock = Now;
        var expiring = new InMemoryGrantStore(() => clock);
        var old = await expiring.SaveAsync(Write("user", "read", "note", "n1", expiresAt: Now), TestCt);
        clock = Now.AddMinutes(1);
        var again = await expiring.SaveAsync(Write("user", "read", "note", "n1"), TestCt);
        var active = await expiring.ListAsync(ct: TestCt);
        Assert.Single(active);
        Assert.Equal(again.Id, active[0].Id);
        Assert.NotEqual(old.Id, again.Id);

        var races = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => CatchAsync(() => store.SaveAsync(Write("race", "read", "note", "n1"), TestCt))));
        Assert.Single(races, r => r is not null);
        Assert.Single(await store.ListAsync(new GrantListQuery { Subject = "race" }, TestCt));

        var saved = await store.SaveAsync(Write("user", "read", "ledger", "e1"), TestCt);
        await store.RevokeAsync(saved.Id, TestCt);
        var second = await store.RevokeAsync(saved.Id, TestCt);
        Assert.NotNull(second.RevokedAt);
        await store.SaveAsync(Write("user", "read", "ledger", "e1"), TestCt);
        await Assert.ThrowsAsync<NotFoundException>(() => store.RevokeAsync(Guid.NewGuid(), TestCt));

        await store.SaveAsync(Write("hidden", "read", "note", "n1", expiresAt: Now.AddMinutes(-1)), TestCt);
        var listed = await store.ListAsync(ct: TestCt);
        Assert.DoesNotContain(listed, g => g.Subject == "hidden");
        Assert.DoesNotContain(listed, g => g.Id == saved.Id);
    }

    [Fact]
    public async Task Metrics_OmitSubject_ThrowingAuditDoesNotFailSave_StoreErrorIsNotDeny()
    {
        var metrics = new MetricsService();
        var logger = new ListLogger();
        var audit = new ThrowingAudit();
        var store = Store();
        var auth = new ResourceAuthorization(store, metrics, logger, audit);
        var saved = await auth.SaveAsync(Write("secret-subject", "read", "note", "n1", grantedBy: "editor"), TestCt);
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.True(await auth.IsAllowedAsync(Check("secret-subject", "read", "note", "n1"), TestCt));
        Assert.False(await auth.IsAllowedAsync(Check("secret-subject", "write", "note", "n1"), TestCt));
        Assert.DoesNotContain(metrics.Counters.Values, c => c.Tags?.Values.Any(v => v.Contains("secret-subject", StringComparison.Ordinal)) == true);
        Assert.DoesNotContain(logger.Information, m => m.Contains("secret-subject", StringComparison.Ordinal));
        Assert.Contains(logger.Debug, m => m.Contains("secret-subject", StringComparison.Ordinal));

        var broken = new ResourceAuthorization(new ThrowingStore(), metrics, logger, audit);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => broken.IsAllowedAsync(Check("secret-subject", "read", "note", "n1"), TestCt));
        Assert.Contains("secret-subject", ex.Message, StringComparison.Ordinal);
        Assert.Contains(metrics.Counters.Values, c => c.Tags?["error_message"] == ResourceAuthorization.StoreFailedMessage);
    }

    private static InMemoryGrantStore Store() => new(() => Now);

    private static GrantWrite Write(string subject, string action, string type, string? resourceId = "n1", Guid? tenant = null, DateTime? expiresAt = null, string? grantedBy = null)
        => new() { Subject = subject, Action = action, ResourceType = type, ResourceId = resourceId, TenantId = tenant, ExpiresAt = expiresAt, GrantedBy = grantedBy };

    private static GrantCheck Check(string subject, string action, string type, string? resourceId, Guid? tenant = null)
        => new() { Subject = subject, Action = action, ResourceType = type, ResourceId = resourceId, TenantId = tenant };

    private static async Task<Grant?> CatchAsync(Func<Task<Grant>> call)
    {
        try {
            return await call();
        }
        catch (ConflictException) {
            return null;
        }
    }

    private sealed class ThrowingStore : IGrantStore
    {
        public string HealthCheckName => "authorization.grants";

        public Task<Grant> SaveAsync(GrantWrite write, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<Grant> RevokeAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> IsAllowedAsync(GrantCheck check, CancellationToken ct = default) => throw new InvalidOperationException("failed for " + check.Subject);

        public Task<IReadOnlyList<Grant>> ListAsync(GrantListQuery? query = null, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class ThrowingAudit : IAuditRecorder
    {
        public void RecordChange(AuditChange change) => throw new InvalidOperationException("audit");

        public Task RecordChangeAsync(AuditChange change, CancellationToken ct = default) => throw new InvalidOperationException("audit");

        public void RecordChanges(IEnumerable<AuditChange> changes) => throw new InvalidOperationException("audit");

        public Task RecordChangesAsync(IEnumerable<AuditChange> changes, CancellationToken ct = default) => throw new InvalidOperationException("audit");

        public void RecordEvent(AuditEvent evt) => throw new InvalidOperationException("audit");

        public Task RecordEventAsync(AuditEvent evt, CancellationToken ct = default) => throw new InvalidOperationException("audit");

        public void RecordEvents(IEnumerable<AuditEvent> events) => throw new InvalidOperationException("audit");

        public Task RecordEventsAsync(IEnumerable<AuditEvent> events, CancellationToken ct = default) => throw new InvalidOperationException("audit");
    }

    private sealed class ListLogger : ILogger
    {
        public List<string> Information { get; } = [];

        public List<string> Debug { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var text = formatter(state, exception);
            if (logLevel == LogLevel.Information)
                Information.Add(text);
            if (logLevel == LogLevel.Debug)
                Debug.Add(text);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}
