using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Idempotency;
using Lyo.Idempotency.Postgres;
using Lyo.MessageQueue;
using Lyo.Outbox.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lyo.Outbox.Dispatcher.Tests;

[Collection("outbox")]
public sealed class OutboxTests(OutboxPostgresFixture fixture) : IClassFixture<OutboxPostgresFixture>
{
    [Fact]
    public async Task Enqueue_RollbackLeavesNoRow_CommitLeavesOneInProgress()
    {
        var (store, outbox) = Pair();
        var message = Message("email", "one"u8.ToArray());
        await using var conn = await OpenAsync();
        await using (var tx = await conn.BeginTransactionAsync(TestCt)) {
            var rolled = await outbox.EnqueueAsync(tx, message, TestCt);
            Assert.True(rolled.Inserted);
            await tx.RollbackAsync(TestCt);
        }

        Assert.Equal(0, await CountAsync(message));
        await using var tx2 = await conn.BeginTransactionAsync(TestCt);
        var kept = await outbox.EnqueueAsync(tx2, message, TestCt);
        await tx2.CommitAsync(TestCt);
        Assert.True(kept.Inserted);
        Assert.Equal(1, await CountAsync(message));
        Assert.Equal("in_progress", await StateAsync(kept.Id));
        _ = store;
    }

    [Fact]
    public async Task Enqueue_SamePayload_IsDuplicate_AndLoserTransactionCanStillCommit()
    {
        var (_, outbox) = Pair();
        var message = Message("email", "same"u8.ToArray());
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => EnqueueAndMaybeProbeAsync(outbox, message)));
        Assert.Single(results, r => r.Inserted);
        Assert.Equal(1, await CountAsync(message));
        Assert.Equal(1, await ProbeCountAsync(results.Single(r => !r.Inserted).Id));
    }

    [Fact]
    public async Task Enqueue_DifferentPayload_RollsBackBusinessRow_AndScopeOverCapLeavesNoRow()
    {
        var (store, outbox) = Pair(new() { MaxScopeLength = 1024, MaxPayloadBytes = 4 });
        var probe = Guid.NewGuid();
        var caller = "caller-" + Guid.NewGuid().ToString("N");
        await using var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(TestCt);
        await using (var insert = new NpgsqlCommand("INSERT INTO idem_probe (id) VALUES (@id)", conn, tx)) {
            insert.Parameters.AddWithValue("id", probe);
            await insert.ExecuteNonQueryAsync(TestCt);
        }

        await outbox.EnqueueAsync(
            tx, new() {
                Destination = "email",
                Payload = "abcd"u8.ToArray(),
                CallerScope = caller,
                Key = "k"
            }, TestCt);

        await Assert.ThrowsAsync<ConflictException>(() => outbox.EnqueueAsync(
            tx, new() {
                Destination = "email",
                Payload = "abce"u8.ToArray(),
                CallerScope = caller,
                Key = "k"
            }, TestCt));

        await Assert.ThrowsAnyAsync<Exception>(() => tx.CommitAsync(TestCt));
        Assert.Equal(0, await ProbeCountAsync(probe));
        var tooLong = new string('s', 1020);
        await using var tx2 = await conn.BeginTransactionAsync(TestCt);
        await Assert.ThrowsAsync<ArgumentException>(() => outbox.EnqueueAsync(
            tx2, new() {
                Destination = "email",
                Payload = "ok"u8.ToArray(),
                CallerScope = tooLong,
                Key = "k2"
            }, TestCt));

        await tx2.RollbackAsync(TestCt);
        Assert.Equal(0, await CountScopeAsync("outbox/email/" + tooLong));
        await using var tx3 = await conn.BeginTransactionAsync(TestCt);
        await Assert.ThrowsAsync<ArgumentException>(() => outbox.EnqueueAsync(
            tx3, new() {
                Destination = "a/b",
                Payload = "ok"u8.ToArray(),
                CallerScope = "c",
                Key = "k3"
            }, TestCt));

        await tx3.RollbackAsync(TestCt);
        _ = store;
    }

    [Fact]
    public async Task Dispatcher_SendsOnce_ThenSkipsCompleted()
    {
        var (store, outbox, queue, dispatcher) = Dispatch();
        await ClearOutboxAsync();
        var message = Message("email", "body"u8.ToArray());
        await CommitAsync(outbox, message);
        await dispatcher.DispatchOnceAsync(TestCt);
        await dispatcher.DispatchOnceAsync(TestCt);
        Assert.Equal(1, queue.Sends);
        Assert.Equal("completed", await StateAsync(await IdAsync(message)));
        _ = store;
    }

    [Fact]
    public async Task Dispatcher_FalseSend_RetriesLater()
    {
        var now = DateTime.UtcNow;
        var (store, outbox, queue, dispatcher) = Dispatch(
            () => now, o => {
                o.Backoff = TimeSpan.FromSeconds(5);
                o.MaxAttempts = 3;
            });

        await ClearOutboxAsync();
        var message = Message("email", "body"u8.ToArray());
        await CommitAsync(outbox, message);
        queue.Result = false;
        await dispatcher.DispatchOnceAsync(TestCt);
        Assert.Equal("in_progress", await StateAsync(await IdAsync(message)));
        Assert.Equal(1, await AttemptsAsync(await IdAsync(message)));
        now = now.AddSeconds(6);
        queue.Result = true;
        await dispatcher.DispatchOnceAsync(TestCt);
        Assert.Equal("completed", await StateAsync(await IdAsync(message)));
        Assert.Equal(2, queue.Sends);
        _ = store;
    }

    [Fact]
    public async Task Dispatcher_MaxAttempts_Fails_ReplayFailed_MakesItEligible_ApiRowUnchanged()
    {
        var (store, outbox, queue, dispatcher) = Dispatch(configure: o => o.MaxAttempts = 1);
        await ClearOutboxAsync();
        var message = Message("email", "body"u8.ToArray());
        await CommitAsync(outbox, message);
        queue.Result = false;
        await dispatcher.DispatchOnceAsync(TestCt);
        var id = await IdAsync(message);
        Assert.Equal("failed", await StateAsync(id));
        Assert.True(await outbox.ReplayFailedAsync(id, TestCt));
        Assert.Equal("in_progress", await StateAsync(id));
        queue.Result = true;
        await dispatcher.DispatchOnceAsync(TestCt);
        Assert.Equal("completed", await StateAsync(id));
        var api = await store.TryBeginAsync("api|" + Guid.NewGuid().ToString("N") + "|POST|/x", "k", IdempotencyHash.Of("a"u8.ToArray()), ct: TestCt);
        Assert.True(await store.FailAsync(api.Id, api.LeaseToken!.Value, 500, null, null, TestCt));
        Assert.False(await outbox.ReplayFailedAsync(api.Id, TestCt));
        Assert.Equal("failed", await StateAsync(api.Id));
    }

    [Fact]
    public async Task TwoDispatchers_OneSendWhileLeaseHeld()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (store, outbox, queue, dispatcher) = Dispatch();
        await ClearOutboxAsync();
        var otherQueue = new FakeQueue();
        var other = new OutboxDispatcher(new(), store, otherQueue);
        var message = Message("email", "body"u8.ToArray());
        await CommitAsync(outbox, message);
        queue.Send = async (_, _) => {
            started.TrySetResult();
            await release.Task;
            return true;
        };

        var first = dispatcher.DispatchOnceAsync(TestCt);
        await started.Task.WaitAsync(TestCt);
        await other.DispatchOnceAsync(TestCt);
        Assert.Equal(0, otherQueue.Sends);
        release.TrySetResult();
        await first;
        Assert.Equal(1, queue.Sends);
    }

    [Fact]
    public async Task StolenLease_RetryDoesNotClearWinner_StaleCompleteDoesNothing()
    {
        var now = DateTime.UtcNow;
        var (store, outbox, queueA, dispatcherA) = Dispatch(() => now);
        await ClearOutboxAsync();
        var queueB = new FakeQueue { Result = true };
        var dispatcherB = new OutboxDispatcher(new() { Lease = TimeSpan.FromSeconds(30) }, store, queueB, utcNow: () => now);
        var message = Message("email", "body"u8.ToArray());
        await CommitAsync(outbox, message);
        queueA.Send = async (_, _) => {
            now = now.AddMinutes(1);
            await dispatcherB.DispatchOnceAsync(TestCt);
            return false;
        };

        await dispatcherA.DispatchOnceAsync(TestCt);
        var id = await IdAsync(message);
        Assert.Equal("completed", await StateAsync(id));
        Assert.Equal(0, await AttemptsAsync(id));
        Assert.Equal(1, queueB.Sends);
        Assert.False(await store.CompleteAsync(id, Guid.NewGuid(), 200, null, null, TestCt));
    }

    [Fact]
    public async Task SendThrow_IsRetryNotComplete()
    {
        var (store, outbox, queue, dispatcher) = Dispatch();
        await ClearOutboxAsync();
        var message = Message("email", "body"u8.ToArray());
        await CommitAsync(outbox, message);
        queue.Throw = true;
        await dispatcher.DispatchOnceAsync(TestCt);
        Assert.Equal("in_progress", await StateAsync(await IdAsync(message)));
        Assert.Equal(1, await AttemptsAsync(await IdAsync(message)));
        _ = store;
    }

    [Fact]
    public async Task Health_ChecksStore_NotBroker()
    {
        var (_, outbox) = Pair();
        Assert.Equal("outbox", outbox.HealthCheckName);
        var health = await outbox.CheckHealthAsync(TestCt);
        Assert.True(health.IsHealthy);
    }

    [Fact]
    public void AddPostgresOutbox_WithoutPostgresStore_ThrowsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddPostgresOutbox();
        using var provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOutbox>());
    }

    private (PostgresIdempotencyStore Store, PostgresOutbox Outbox) Pair(IdempotencyOptions? limits = null)
    {
        var options = new PostgresIdempotencyOptions { ConnectionString = fixture.ConnectionString };
        if (limits is not null)
            options.Limits = limits;

        var store = new PostgresIdempotencyStore(options);
        return (store, new(store));
    }

    private (PostgresIdempotencyStore Store, PostgresOutbox Outbox, FakeQueue Queue, OutboxDispatcher Dispatcher) Dispatch(
        Func<DateTime>? clock = null,
        Action<OutboxDispatcherOptions>? configure = null)
    {
        var options = new PostgresIdempotencyOptions { ConnectionString = fixture.ConnectionString };
        var store = new PostgresIdempotencyStore(options, clock);
        var outbox = new PostgresOutbox(store);
        var queue = new FakeQueue();
        var dispatcherOptions = new OutboxDispatcherOptions();
        configure?.Invoke(dispatcherOptions);
        var dispatcher = new OutboxDispatcher(dispatcherOptions, store, queue, utcNow: clock);
        return (store, outbox, queue, dispatcher);
    }

    private async Task CommitAsync(PostgresOutbox outbox, OutboxMessage message)
    {
        await using var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(TestCt);
        Assert.True((await outbox.EnqueueAsync(tx, message, TestCt)).Inserted);
        await tx.CommitAsync(TestCt);
    }

    private async Task<OutboxEnqueueResult> EnqueueAndMaybeProbeAsync(PostgresOutbox outbox, OutboxMessage message)
    {
        await using var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(TestCt);
        var result = await outbox.EnqueueAsync(tx, message, TestCt);
        if (!result.Inserted) {
            await using var insert = new NpgsqlCommand("INSERT INTO idem_probe (id) VALUES (@id)", conn, tx);
            insert.Parameters.AddWithValue("id", result.Id);
            await insert.ExecuteNonQueryAsync(TestCt);
        }

        await tx.CommitAsync(TestCt);
        return result;
    }

    private async Task ClearOutboxAsync()
    {
        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM idempotency.entry WHERE kind = 'outbox'", conn);
        await cmd.ExecuteNonQueryAsync(TestCt);
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var conn = new NpgsqlConnection(fixture.ConnectionString);
        await conn.OpenAsync(TestCt);
        return conn;
    }

    private async Task<int> CountAsync(OutboxMessage message)
    {
        var scope = "outbox/" + message.Destination + "/" + message.CallerScope;
        return await CountScopeAsync(scope, message.Key);
    }

    private async Task<int> CountScopeAsync(string scope, string? key = null)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand(
            key is null ? "SELECT COUNT(*) FROM idempotency.entry WHERE scope = @scope" : "SELECT COUNT(*) FROM idempotency.entry WHERE scope = @scope AND key = @key", conn);

        cmd.Parameters.AddWithValue("scope", scope);
        if (key is not null)
            cmd.Parameters.AddWithValue("key", key);

        return (int)(long)(await cmd.ExecuteScalarAsync(TestCt))!;
    }

    private async Task<string> StateAsync(Guid id)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT state FROM idempotency.entry WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        return (string)(await cmd.ExecuteScalarAsync(TestCt))!;
    }

    private async Task<int> AttemptsAsync(Guid id)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT attempt_count FROM idempotency.entry WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        return (int)(await cmd.ExecuteScalarAsync(TestCt))!;
    }

    private async Task<Guid> IdAsync(OutboxMessage message)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT id FROM idempotency.entry WHERE scope = @scope AND key = @key", conn);
        cmd.Parameters.AddWithValue("scope", "outbox/" + message.Destination + "/" + message.CallerScope);
        cmd.Parameters.AddWithValue("key", message.Key);
        return (Guid)(await cmd.ExecuteScalarAsync(TestCt))!;
    }

    private async Task<int> ProbeCountAsync(Guid id)
    {
        await using var conn = await OpenAsync();
        await using (var setup = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS idem_probe (id uuid PRIMARY KEY)", conn))
            await setup.ExecuteNonQueryAsync(TestCt);

        await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM idem_probe WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        return (int)(long)(await cmd.ExecuteScalarAsync(TestCt))!;
    }

    private static OutboxMessage Message(string destination, byte[] payload)
        => new() {
            Destination = destination,
            Payload = payload,
            CallerScope = "pay-" + Guid.NewGuid().ToString("N"),
            Key = "k"
        };

    private sealed class FakeQueue : IMqService
    {
        public int Sends { get; private set; }

        public bool Result { get; set; } = true;

        public bool Throw { get; set; }

        public Func<string, byte[], Task<bool>>? Send { get; set; }

        public string HealthCheckName => "mq";

        public Task<bool> SendToQueue(string queueName, byte[] data)
        {
            Sends++;
            if (Throw)
                throw new InvalidOperationException("broker down");

            return Send?.Invoke(queueName, data) ?? Task.FromResult(Result);
        }

        public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero));

        public Task ConnectAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;

        public bool IsConnected() => true;

        public Task<bool> CreateQueue(
            string queueName,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null,
            CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> ClearQueue(string queueName, CancellationToken ct = default) => Task.FromResult(true);

        public Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<QueuePeekMessage>>([]);

        public Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> CreateExchange(
            string exchangeName,
            string exchangeType = "direct",
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null,
            CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data) => Task.FromResult(true);
    }
}