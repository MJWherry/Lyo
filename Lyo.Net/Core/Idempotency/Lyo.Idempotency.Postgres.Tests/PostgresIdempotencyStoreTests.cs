using Lyo.Exceptions.Models;
using Npgsql;

namespace Lyo.Idempotency.Postgres.Tests;

public sealed class PostgresIdempotencyStoreTests(IdempotencyPostgresFixture fixture) : IClassFixture<IdempotencyPostgresFixture>
{
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TryBegin_SameHash_Replays_DifferentHash_Conflicts()
    {
        var store = fixture.Store;
        var scope = Scope();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var began = await store.TryBeginAsync(scope, "k", hash, ct: TestCt);
        Assert.True(await store.CompleteAsync(began.Id, began.LeaseToken!.Value, 201, "a"u8.ToArray(), "text/plain", TestCt));

        var replay = await store.TryBeginAsync(scope, "k", hash, ct: TestCt);
        Assert.Equal(IdempotencyOutcome.Replay, replay.Outcome);
        Assert.Equal(201, replay.ResponseStatus);
        Assert.Equal("a"u8.ToArray(), replay.ResponseBody);

        var conflict = await store.TryBeginAsync(scope, "k", IdempotencyHash.Of("b"u8.ToArray()), ct: TestCt);
        Assert.Equal(IdempotencyOutcome.Conflict, conflict.Outcome);
    }

    [Fact]
    public async Task TryBegin_WhileInProgress_DoesNotSteal()
    {
        var store = fixture.Store;
        var scope = Scope();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var first = await store.TryBeginAsync(scope, "k", hash, ct: TestCt);
        var second = await store.TryBeginAsync(scope, "k", hash, ct: TestCt);

        Assert.Equal(IdempotencyOutcome.Began, first.Outcome);
        Assert.Equal(IdempotencyOutcome.InProgress, second.Outcome);
    }

    [Fact]
    public async Task TryBegin_NoTransaction_CommitsInProgressBeforeReturn()
    {
        var store = fixture.Store;
        var scope = Scope();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var began = await store.TryBeginAsync(scope, "k", hash, ct: TestCt);

        await using var conn = await OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT state FROM idempotency.entry WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", began.Id);
        var state = (string?)await cmd.ExecuteScalarAsync(TestCt);
        Assert.Equal("in_progress", state);
    }

    [Fact]
    public async Task TryBegin_CallerRollback_LeavesNoRow()
    {
        var store = fixture.Store;
        var scope = Scope();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        await using var conn = await OpenAsync();
        await using (var tx = await conn.BeginTransactionAsync(TestCt)) {
            var began = await store.TryBeginAsync(scope, "k", hash, transaction: tx, ct: TestCt);
            Assert.Equal(IdempotencyOutcome.Began, began.Outcome);
            await tx.RollbackAsync(TestCt);
        }

        var again = await store.TryBeginAsync(scope, "k", hash, ct: TestCt);
        Assert.Equal(IdempotencyOutcome.Began, again.Outcome);
    }

    [Fact]
    public async Task TryBegin_DuplicateInsideCallerTransaction_KeepsTheTransactionOpen()
    {
        var store = fixture.Store;
        var scope = Scope();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var probe = Guid.NewGuid();
        await using var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(TestCt);
        await using (var insert = new NpgsqlCommand("INSERT INTO idem_probe (id) VALUES (@id)", conn, tx)) {
            insert.Parameters.AddWithValue("id", probe);
            await insert.ExecuteNonQueryAsync(TestCt);
        }

        Assert.Equal(IdempotencyOutcome.Began, (await store.TryBeginAsync(scope, "k", hash, transaction: tx, ct: TestCt)).Outcome);
        Assert.Equal(IdempotencyOutcome.InProgress, (await store.TryBeginAsync(scope, "k", hash, transaction: tx, ct: TestCt)).Outcome);
        await tx.CommitAsync(TestCt);

        await using var check = new NpgsqlCommand("SELECT COUNT(*) FROM idem_probe WHERE id = @id", conn);
        check.Parameters.AddWithValue("id", probe);
        Assert.Equal(1L, (long)(await check.ExecuteScalarAsync(TestCt))!);
    }

    [Fact]
    public async Task TryBegin_ExpiredAndLive_UsesTheLiveRow()
    {
        var store = Clock();
        var scope = Scope();
        var oldHash = IdempotencyHash.Of("old"u8.ToArray());
        var first = await store.TryBeginAsync(scope, "k", oldHash, expiresAt: Now, ct: TestCt);
        Assert.True(await store.CompleteAsync(first.Id, first.LeaseToken!.Value, 200, "old-body"u8.ToArray(), "text/plain", TestCt));
        await store.SweepExpiredAsync(TestCt);

        var liveHash = IdempotencyHash.Of("new"u8.ToArray());
        var live = await store.TryBeginAsync(scope, "k", liveHash, ct: TestCt);
        var again = await store.TryBeginAsync(scope, "k", liveHash, ct: TestCt);
        Assert.Equal(IdempotencyOutcome.Began, live.Outcome);
        Assert.Equal(IdempotencyOutcome.InProgress, again.Outcome);
        Assert.Null(again.ResponseBody);
    }

    [Fact]
    public async Task Sweep_ExpiresStuckApiRow_AndLeavesOutbox()
    {
        var store = Clock();
        var scope = Scope();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        await store.TryBeginAsync(scope, "k", hash, expiresAt: Now, ct: TestCt);

        await using var conn = await OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(TestCt);
        var inserted = await store.InsertOutboxAsync(tx, "email", "pay", "k-out", "body"u8.ToArray(), TestCt);
        await tx.CommitAsync(TestCt);
        Assert.True(inserted.Inserted);

        await store.SweepExpiredAsync(TestCt);
        Assert.Equal(IdempotencyOutcome.Began, (await store.TryBeginAsync(scope, "k", hash, ct: TestCt)).Outcome);

        await using var tx2 = await conn.BeginTransactionAsync(TestCt);
        var again = await store.InsertOutboxAsync(tx2, "email", "pay", "k-out", "body"u8.ToArray(), TestCt);
        await tx2.RollbackAsync(TestCt);
        Assert.False(again.Inserted);
    }

    [Fact]
    public async Task TryBegin_Concurrent_OneOwner()
    {
        var store = fixture.Store;
        var scope = Scope();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => store.TryBeginAsync(scope, "k", hash, ct: TestCt)));
        Assert.Single(results, r => r.Outcome == IdempotencyOutcome.Began);
        Assert.All(results.Where(r => r.Outcome != IdempotencyOutcome.Began), r => Assert.Equal(IdempotencyOutcome.InProgress, r.Outcome));
    }

    [Fact]
    public async Task Complete_WrongLease_LeavesResponse()
    {
        var store = fixture.Store;
        var scope = Scope();
        var hash = IdempotencyHash.Of("a"u8.ToArray());
        var began = await store.TryBeginAsync(scope, "k", hash, ct: TestCt);
        Assert.True(await store.CompleteAsync(began.Id, began.LeaseToken!.Value, 200, "kept"u8.ToArray(), "text/plain", TestCt));
        Assert.False(await store.CompleteAsync(began.Id, Guid.NewGuid(), 500, "nope"u8.ToArray(), "text/plain", TestCt));
        var replay = await store.TryBeginAsync(scope, "k", hash, ct: TestCt);
        Assert.Equal("kept"u8.ToArray(), replay.ResponseBody);
    }

    [Fact]
    public async Task InsertOutbox_DifferentHash_RollsBackAndThrows()
    {
        var store = fixture.Store;
        var probe = Guid.NewGuid();
        await using var conn = await OpenAsync();
        var caller = Scope();
        var thrown = false;
        await using var tx = await conn.BeginTransactionAsync(TestCt);
        await using (var insert = new NpgsqlCommand("INSERT INTO idem_probe (id) VALUES (@id)", conn, tx)) {
            insert.Parameters.AddWithValue("id", probe);
            await insert.ExecuteNonQueryAsync(TestCt);
        }

        await store.InsertOutboxAsync(tx, "email", caller, "k", "one"u8.ToArray(), TestCt);
        try {
            await store.InsertOutboxAsync(tx, "email", caller, "k", "two"u8.ToArray(), TestCt);
        }
        catch (ConflictException) {
            thrown = true;
        }

        Assert.True(thrown);
        await Assert.ThrowsAnyAsync<Exception>(() => tx.CommitAsync(TestCt));
        await using var check = new NpgsqlCommand("SELECT COUNT(*) FROM idem_probe WHERE id = @id", conn);
        check.Parameters.AddWithValue("id", probe);
        Assert.Equal(0L, (long)(await check.ExecuteScalarAsync(TestCt))!);
    }

    [Fact]
    public async Task CheckHealth_AgainstContainer_IsHealthy()
    {
        var health = await fixture.Store.CheckHealthAsync(TestCt);
        Assert.True(health.IsHealthy);
        Assert.Equal("idempotency", fixture.Store.HealthCheckName);
    }

    private PostgresIdempotencyStore Clock()
        => new(new PostgresIdempotencyOptions { ConnectionString = fixture.ConnectionString }, () => Now);

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var conn = new NpgsqlConnection(fixture.ConnectionString);
        await conn.OpenAsync(TestCt);
        return conn;
    }

    private static string Scope() => "api|" + Guid.NewGuid().ToString("N") + "|POST|/pay";
}
