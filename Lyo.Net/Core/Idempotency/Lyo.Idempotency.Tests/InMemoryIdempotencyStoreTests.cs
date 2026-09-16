using System.Data;
using System.Data.Common;
using System.Text;
using Lyo.Health;
using Lyo.Idempotency;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.Idempotency.Tests;

public sealed class InMemoryIdempotencyStoreTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TryBegin_SameKeyAndHash_ReplaysAndDoesNotRunAgain()
    {
        var store = Store();
        var hash = IdempotencyHash.Of("body"u8.ToArray());
        var runs = 0;
        var first = await RunOnceAsync(store, "api|user|POST|/pay", "k1", hash, () => {
            runs++;
            return (200, "ok"u8.ToArray(), "text/plain");
        });
        var second = await RunOnceAsync(store, "api|user|POST|/pay", "k1", hash, () => {
            runs++;
            return (200, "ok"u8.ToArray(), "text/plain");
        });

        Assert.Equal(IdempotencyOutcome.Began, first.Outcome);
        Assert.Equal(IdempotencyOutcome.Replay, second.Outcome);
        Assert.Equal(200, second.ResponseStatus);
        Assert.Equal("ok"u8.ToArray(), second.ResponseBody);
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task TryBegin_DifferentHash_ReturnsConflictAndDoesNotRun()
    {
        var store = Store();
        var scope = "api|user|POST|/pay";
        await RunOnceAsync(store, scope, "k1", IdempotencyHash.Of("a"u8.ToArray()), () => (201, "a"u8.ToArray(), "text/plain"));
        var runs = 0;
        var second = await store.TryBeginAsync(scope, "k1", IdempotencyHash.Of("b"u8.ToArray()), ct: TestCt);
        if (second.Outcome == IdempotencyOutcome.Began)
            runs++;

        Assert.Equal(IdempotencyOutcome.Conflict, second.Outcome);
        Assert.Equal(0, runs);
    }

    [Fact]
    public async Task TryBegin_WhileInProgress_DoesNotSteal()
    {
        var store = Store();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var first = await store.TryBeginAsync("scope", "key", hash, ct: TestCt);
        var second = await store.TryBeginAsync("scope", "key", hash, ct: TestCt);

        Assert.Equal(IdempotencyOutcome.Began, first.Outcome);
        Assert.Equal(IdempotencyOutcome.InProgress, second.Outcome);
        Assert.NotEqual(first.LeaseToken, second.LeaseToken);
    }

    [Fact]
    public async Task TryBegin_ExpiryEqualToNow_DoesNotReplayUntilSwept()
    {
        var clock = Now;
        var store = Store(() => clock);
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var began = await store.TryBeginAsync("scope", "key", hash, expiresAt: Now, ct: TestCt);
        Assert.True(await store.CompleteAsync(began.Id, began.LeaseToken!.Value, 200, "old"u8.ToArray(), "text/plain", TestCt));

        var blocked = await store.TryBeginAsync("scope", "key", hash, ct: TestCt);
        Assert.NotEqual(IdempotencyOutcome.Replay, blocked.Outcome);
        Assert.NotEqual(IdempotencyOutcome.Began, blocked.Outcome);

        await store.SweepExpiredAsync(TestCt);
        var again = await store.TryBeginAsync("scope", "key", hash, ct: TestCt);
        Assert.Equal(IdempotencyOutcome.Began, again.Outcome);
    }

    [Fact]
    public async Task TryBegin_ConcurrentClaims_OneOwner()
    {
        var store = Store();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => store.TryBeginAsync("scope", "key", hash, ct: TestCt)));

        Assert.Single(results, r => r.Outcome == IdempotencyOutcome.Began);
        Assert.All(results.Where(r => r.Outcome != IdempotencyOutcome.Began), r => Assert.Equal(IdempotencyOutcome.InProgress, r.Outcome));
    }

    [Fact]
    public async Task TryBegin_ExpiredRowAndLiveRow_UsesTheLiveRow()
    {
        var clock = Now;
        var store = Store(() => clock);
        var hash = IdempotencyHash.Of("old"u8.ToArray());
        var first = await store.TryBeginAsync("scope", "key", hash, expiresAt: Now, ct: TestCt);
        Assert.True(await store.CompleteAsync(first.Id, first.LeaseToken!.Value, 200, "old-body"u8.ToArray(), "text/plain", TestCt));
        await store.SweepExpiredAsync(TestCt);

        clock = Now.AddMinutes(1);
        var liveHash = IdempotencyHash.Of("new"u8.ToArray());
        var live = await store.TryBeginAsync("scope", "key", liveHash, ct: TestCt);
        var again = await store.TryBeginAsync("scope", "key", liveHash, ct: TestCt);

        Assert.Equal(IdempotencyOutcome.InProgress, again.Outcome);
        Assert.Null(again.ResponseBody);
        Assert.NotEqual(first.Id, live.Id);
    }

    [Fact]
    public async Task Complete_WrongLeaseToken_LeavesResponseUnchanged()
    {
        var store = Store();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var began = await store.TryBeginAsync("scope", "key", hash, ct: TestCt);
        Assert.True(await store.CompleteAsync(began.Id, began.LeaseToken!.Value, 200, "kept"u8.ToArray(), "text/plain", TestCt));

        Assert.False(await store.CompleteAsync(began.Id, Guid.NewGuid(), 500, "nope"u8.ToArray(), "text/plain", TestCt));
        var replay = await store.TryBeginAsync("scope", "key", hash, ct: TestCt);
        Assert.Equal(IdempotencyOutcome.Replay, replay.Outcome);
        Assert.Equal("kept"u8.ToArray(), replay.ResponseBody);
    }

    [Fact]
    public async Task TryBegin_WithTransaction_Throws()
    {
        var store = Store();
        await Assert.ThrowsAsync<NotSupportedException>(() => store.TryBeginAsync("scope", "key", IdempotencyHash.Of("a"u8.ToArray()), transaction: new UnusedTransaction(), ct: TestCt));
    }

    [Fact]
    public async Task Complete_OverCap_OmitsBodyAndDoesNotReexecute()
    {
        var store = new InMemoryIdempotencyStore(new IdempotencyOptions { MaxResponseBytes = 4 }, () => Now);
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var runs = 0;
        await RunOnceAsync(store, "scope", "key", hash, () => {
            runs++;
            return (200, "too-big"u8.ToArray(), "text/plain");
        });
        var replay = await RunOnceAsync(store, "scope", "key", hash, () => {
            runs++;
            return (200, "too-big"u8.ToArray(), "text/plain");
        });

        Assert.Equal(IdempotencyOutcome.Replay, replay.Outcome);
        Assert.True(replay.ResponseOmitted);
        Assert.Null(replay.ResponseBody);
        Assert.Equal(200, replay.ResponseStatus);
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task TryBegin_OutboxPrefix_Throws_ApiSubjectScopeDoesNot()
    {
        var store = Store();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        await Assert.ThrowsAsync<ArgumentException>(() => store.TryBeginAsync("outbox/email/pay", "key", hash, ct: TestCt));
        var began = await store.TryBeginAsync("api|outbox/user|POST|/pay", "key", hash, ct: TestCt);
        Assert.Equal(IdempotencyOutcome.Began, began.Outcome);
    }

    [Fact]
    public async Task TryBegin_LocalExpiry_Throws()
    {
        var store = Store();
        var local = DateTime.SpecifyKind(Now, DateTimeKind.Local);
        await Assert.ThrowsAsync<ArgumentException>(() => store.TryBeginAsync("scope", "key", IdempotencyHash.Of("a"u8.ToArray()), expiresAt: local, ct: TestCt));
    }

    [Fact]
    public async Task Observing_DenyLogAndErrorMetric_OmitSubject()
    {
        var metrics = new MetricsService();
        var logger = new ListLogger();
        var inner = new ThrowingStore("secret-subject");
        var store = new ObservingIdempotencyStore(inner, metrics, logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => store.TryBeginAsync("api|secret-subject|POST|/pay", "key", IdempotencyHash.Of("a"u8.ToArray()), ct: TestCt));
        Assert.Contains("secret-subject", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(metrics.Counters.Values, c => c.Tags?.Values.Any(v => v.Contains("secret-subject", StringComparison.Ordinal)) == true);
        Assert.Contains(metrics.Counters.Values, c => c.Tags?["error_message"] == ObservingIdempotencyStore.StoreFailedMessage);
        Assert.DoesNotContain(logger.Information, m => m.Contains("secret-subject", StringComparison.Ordinal));
        Assert.Contains(logger.Debug, m => m.Contains("secret-subject", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CheckHealth_InMemory_IsHealthy()
    {
        var health = await Store().CheckHealthAsync(TestCt);
        Assert.True(health.IsHealthy);
        Assert.Equal("idempotency", Store().HealthCheckName);
    }

    private static InMemoryIdempotencyStore Store(Func<DateTime>? utcNow = null) => new(utcNow: utcNow ?? (() => Now));

    private static async Task<IdempotencyBeginResult> RunOnceAsync(
        IIdempotencyStore store,
        string scope,
        string key,
        byte[] hash,
        Func<(int Status, byte[] Body, string ContentType)> work)
    {
        var begin = await store.TryBeginAsync(scope, key, hash, ct: TestCt);
        if (begin.Outcome != IdempotencyOutcome.Began)
            return begin;

        var (status, body, contentType) = work();
        Assert.True(await store.CompleteAsync(begin.Id, begin.LeaseToken!.Value, status, body, contentType, TestCt));
        return begin;
    }

    private sealed class ThrowingStore(string subject) : IIdempotencyStore
    {
        public string HealthCheckName => "idempotency";

        public Task<IdempotencyBeginResult> TryBeginAsync(string scope, string key, byte[] requestHash, DateTime? expiresAt = null, DbTransaction? transaction = null, CancellationToken ct = default)
            => throw new InvalidOperationException("failed for " + subject);

        public Task<bool> CompleteAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> FailAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> AbandonAsync(string scope, string key, Guid? leaseToken, bool force, CancellationToken ct = default) => throw new NotSupportedException();

        public Task SweepExpiredAsync(CancellationToken ct = default) => throw new NotSupportedException();

        public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => throw new NotSupportedException();
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
            if (exception is not null)
                text += " " + exception.Message;

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

    private sealed class UnusedTransaction : DbTransaction
    {
        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        protected override DbConnection DbConnection => throw new NotSupportedException();

        public override void Commit() { }

        public override void Rollback() { }
    }
}
