using System.Data;
using System.Data.Common;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Metrics;
using Npgsql;

namespace Lyo.Idempotency.Postgres;

/// <summary>
/// Postgres idempotency store. A caller transaction gets a savepoint around the insert so a unique violation can be re-read
/// without aborting the caller's transaction. Outbox rows are inserted only through <see cref="InsertOutboxAsync" />.
/// </summary>
public sealed class PostgresIdempotencyStore : IIdempotencyStore
{
    private readonly PostgresIdempotencyOptions _options;
    private readonly Func<DateTime> _utcNow;
    private readonly IMetrics _metrics;

    /// <summary>Builds a store over <paramref name="options" />.</summary>
    public PostgresIdempotencyStore(PostgresIdempotencyOptions options, Func<DateTime>? utcNow = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
        _metrics = metrics ?? NullMetrics.Instance;
    }

    /// <inheritdoc />
    public string HealthCheckName => "idempotency";

    /// <inheritdoc />
    public async Task<IdempotencyBeginResult> TryBeginAsync(
        string scope,
        string key,
        byte[] requestHash,
        DateTime? expiresAt = null,
        DbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        IdempotencyRules.ThrowIfApiScope(scope, key, requestHash, _options.Limits);
        var now = IdempotencyRules.AsUtc(_utcNow(), "utcNow");
        var expiry = IdempotencyRules.ResolveExpiry(expiresAt, now, _options.Limits.DefaultLifetime);
        var entry = NewEntry(IdempotencyKind.Api, scope, key, requestHash, expiry, now, destination: null, payload: null);
        if (transaction is null)
            return await BeginOwnedAsync(entry, ct).ConfigureAwait(false);

        return await BeginJoinedAsync(entry, transaction, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Inserts an outbox row on <paramref name="transaction" /> and does not commit it. The hash is SHA-256 of <paramref name="payload" />.
    /// A different hash rolls the caller transaction back and throws <see cref="ConflictException" />.
    /// </summary>
    public async Task<IdempotencyOutboxInsert> InsertOutboxAsync(
        DbTransaction transaction,
        string destination,
        string callerScope,
        string key,
        byte[] payload,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(transaction);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(destination);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(callerScope);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(payload);
        if (destination.Contains('/') || destination.Contains('|'))
            throw new ArgumentException("Destination cannot contain '/' or '|'.", nameof(destination));

        if (payload.Length > _options.Limits.MaxPayloadBytes)
            throw new ArgumentException($"Payload length {payload.Length} exceeds {_options.Limits.MaxPayloadBytes}.", nameof(payload));

        var scope = IdempotencyRules.OutboxPrefix + destination + "/" + callerScope;
        IdempotencyRules.ThrowIfScopeAndKey(scope, key, _options.Limits);
        var hash = IdempotencyHash.Of(payload);
        var now = IdempotencyRules.AsUtc(_utcNow(), "utcNow");
        var entry = NewEntry(IdempotencyKind.Outbox, scope, key, hash, expiresAt: null, now, destination, payload);
        var conn = RequireConnection(transaction);
        var inserted = await TryInsertAsync(conn, transaction, entry, joined: true, ct).ConfigureAwait(false);
        if (inserted)
            return new IdempotencyOutboxInsert { Inserted = true, Id = entry.Id, Outcome = IdempotencyOutcome.Began };

        var live = await ReadLiveAsync(conn, transaction, scope, key, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Idempotency unique violation had no live row.");
        if (IdempotencyHash.Matches(live.RequestHash, hash))
            return new IdempotencyOutboxInsert { Inserted = false, Id = live.Id, Outcome = IdempotencyOutcome.Replay };

        _metrics.IncrementCounter("outbox.enqueue", tags: [("outcome", "conflict")]);
        await transaction.RollbackAsync(ct).ConfigureAwait(false);
        throw new ConflictException("An outbox message already exists for this key with a different payload.");
    }

    /// <inheritdoc />
    public Task<bool> CompleteAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default)
        => FinishAsync(id, leaseToken, "completed", status, body, contentType, ct);

    /// <inheritdoc />
    public Task<bool> FailAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default)
        => FinishAsync(id, leaseToken, "failed", status, body, contentType, ct);

    /// <inheritdoc />
    public async Task<bool> AbandonAsync(string scope, string key, Guid? leaseToken, bool force, CancellationToken ct = default)
    {
        IdempotencyRules.ThrowIfScopeAndKey(scope, key, _options.Limits);
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = force
            ? """
              UPDATE idempotency.entry
              SET state = 'expired'
              WHERE scope = @scope AND key = @key AND kind = 'api' AND state IN ('in_progress', 'completed', 'failed')
              """
            : """
              UPDATE idempotency.entry
              SET state = 'expired'
              WHERE scope = @scope AND key = @key AND kind = 'api' AND state IN ('in_progress', 'completed', 'failed')
                AND lease_token = @token
              """;
        cmd.Parameters.AddWithValue("scope", scope);
        cmd.Parameters.AddWithValue("key", key);
        if (!force)
            cmd.Parameters.AddWithValue("token", leaseToken ?? Guid.Empty);

        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 1;
    }

    /// <inheritdoc />
    public async Task SweepExpiredAsync(CancellationToken ct = default)
    {
        var now = IdempotencyRules.AsUtc(_utcNow(), "utcNow");
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE idempotency.entry
            SET state = 'expired'
            WHERE kind = 'api'
              AND state IN ('in_progress', 'completed', 'failed')
              AND expires_at IS NOT NULL
              AND expires_at <= @now
            """;
        cmd.Parameters.AddWithValue("now", now);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var started = DateTime.UtcNow;
        try {
            await using var conn = await OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return HealthResult.Healthy(DateTime.UtcNow - started);
        }
        catch (Exception ex) {
            return HealthResult.Unhealthy(DateTime.UtcNow - started, "Idempotency store probe failed.", exception: ex);
        }
    }

    /// <summary>Claims due outbox rows and commits the new leases before returning. Does not publish.</summary>
    public async Task<IReadOnlyList<IdempotencyOutboxClaim>> ClaimDueOutboxAsync(int batch, TimeSpan lease, CancellationToken ct = default)
    {
        if (batch < 1)
            throw new ArgumentOutOfRangeException(nameof(batch));

        var now = IdempotencyRules.AsUtc(_utcNow(), "utcNow");
        var until = now.Add(lease);
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        var ids = new List<Guid>();
        await using (var select = conn.CreateCommand()) {
            select.Transaction = tx;
            select.CommandText = """
                SELECT id
                FROM idempotency.entry
                WHERE kind = 'outbox'
                  AND state = 'in_progress'
                  AND (available_at IS NULL OR available_at <= @now)
                  AND (lease_until IS NULL OR lease_until <= @now)
                ORDER BY created_timestamp
                LIMIT @batch
                FOR UPDATE SKIP LOCKED
                """;
            select.Parameters.AddWithValue("now", now);
            select.Parameters.AddWithValue("batch", batch);
            await using var reader = await select.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                ids.Add(reader.GetGuid(0));
        }

        var claims = new List<IdempotencyOutboxClaim>();
        foreach (var id in ids) {
            var token = LyoGuid.CreateV7();
            await using var update = conn.CreateCommand();
            update.Transaction = tx;
            update.CommandText = """
                UPDATE idempotency.entry
                SET lease_token = @token, lease_until = @until
                WHERE id = @id
                RETURNING destination, payload, attempt_count
                """;
            update.Parameters.AddWithValue("token", token);
            update.Parameters.AddWithValue("until", until);
            update.Parameters.AddWithValue("id", id);
            await using var reader = await update.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                continue;

            claims.Add(new IdempotencyOutboxClaim {
                Id = id,
                LeaseToken = token,
                Destination = reader.GetString(0),
                Payload = (byte[])reader.GetValue(1),
                AttemptCount = reader.GetInt32(2)
            });
        }

        await tx.CommitAsync(ct).ConfigureAwait(false);
        return claims;
    }

    /// <summary>
    /// Backs off a failed publish. The update matches <paramref name="leaseToken" />. Zero rows means the lease was stolen and nothing changes.
    /// </summary>
    public async Task<bool> ScheduleOutboxRetryAsync(Guid id, Guid leaseToken, DateTime availableAt, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE idempotency.entry
            SET attempt_count = attempt_count + 1,
                available_at = @available,
                lease_token = NULL,
                lease_until = NULL
            WHERE id = @id AND lease_token = @token AND kind = 'outbox' AND state = 'in_progress'
            """;
        cmd.Parameters.AddWithValue("available", IdempotencyRules.AsUtc(availableAt, nameof(availableAt)));
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("token", leaseToken);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 1;
    }

    /// <summary>Returns a failed outbox row to in-progress. API rows and rows that are not failed are left unchanged.</summary>
    public async Task<bool> ReplayFailedOutboxAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE idempotency.entry
            SET state = 'in_progress', attempt_count = 0, lease_token = NULL, lease_until = NULL,
                available_at = NULL, completed_timestamp = NULL, response_status = NULL, response_body = NULL,
                response_content_type = NULL, response_omitted = FALSE
            WHERE id = @id AND kind = 'outbox' AND state = 'failed'
            """;
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 1;
    }

    private async Task<IdempotencyBeginResult> BeginOwnedAsync(IdempotencyEntry entry, CancellationToken ct)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        var inserted = await TryInsertAsync(conn, tx, entry, joined: false, ct).ConfigureAwait(false);
        if (inserted) {
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return IdempotencyDecision.Began(entry);
        }

        await tx.RollbackAsync(ct).ConfigureAwait(false);
        var live = await ReadLiveAsync(conn, transaction: null, entry.Scope, entry.Key, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Idempotency unique violation had no live row.");
        return IdempotencyDecision.FromLive(live, entry.RequestHash, IdempotencyRules.AsUtc(_utcNow(), "utcNow"));
    }

    private async Task<IdempotencyBeginResult> BeginJoinedAsync(IdempotencyEntry entry, DbTransaction transaction, CancellationToken ct)
    {
        var conn = RequireConnection(transaction);
        var inserted = await TryInsertAsync(conn, transaction, entry, joined: true, ct).ConfigureAwait(false);
        if (inserted)
            return IdempotencyDecision.Began(entry);

        var live = await ReadLiveAsync(conn, transaction, entry.Scope, entry.Key, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Idempotency unique violation had no live row.");
        return IdempotencyDecision.FromLive(live, entry.RequestHash, IdempotencyRules.AsUtc(_utcNow(), "utcNow"));
    }

    private async Task<bool> TryInsertAsync(NpgsqlConnection conn, DbTransaction transaction, IdempotencyEntry entry, bool joined, CancellationToken ct)
    {
        var savepoint = "lyo_idem_" + Guid.NewGuid().ToString("N");
        if (joined)
            await ExecAsync(conn, transaction, "SAVEPOINT " + savepoint, ct).ConfigureAwait(false);

        try {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = (NpgsqlTransaction)transaction;
            cmd.CommandText = """
                INSERT INTO idempotency.entry (
                    id, kind, scope, key, request_hash, state, lease_token, lease_until,
                    response_omitted, destination, payload, attempt_count, expires_at, created_timestamp)
                VALUES (
                    @id, @kind, @scope, @key, @hash, @state, @token, @until,
                    FALSE, @destination, @payload, @attempts, @expires, @created)
                """;
            cmd.Parameters.AddWithValue("id", entry.Id);
            cmd.Parameters.AddWithValue("kind", entry.Kind == IdempotencyKind.Outbox ? "outbox" : "api");
            cmd.Parameters.AddWithValue("scope", entry.Scope);
            cmd.Parameters.AddWithValue("key", entry.Key);
            cmd.Parameters.AddWithValue("hash", entry.RequestHash);
            cmd.Parameters.AddWithValue("state", "in_progress");
            cmd.Parameters.AddWithValue("token", (object?)entry.LeaseToken ?? DBNull.Value);
            cmd.Parameters.AddWithValue("until", (object?)entry.LeaseUntil ?? DBNull.Value);
            cmd.Parameters.AddWithValue("destination", (object?)entry.Destination ?? DBNull.Value);
            cmd.Parameters.AddWithValue("payload", (object?)entry.Payload ?? DBNull.Value);
            cmd.Parameters.AddWithValue("attempts", entry.AttemptCount);
            cmd.Parameters.AddWithValue("expires", (object?)entry.ExpiresAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("created", entry.CreatedTimestamp);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            if (joined)
                await ExecAsync(conn, transaction, "RELEASE SAVEPOINT " + savepoint, ct).ConfigureAwait(false);

            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") {
            if (joined)
                await ExecAsync(conn, transaction, "ROLLBACK TO SAVEPOINT " + savepoint, ct).ConfigureAwait(false);

            return false;
        }
    }

    private async Task<bool> FinishAsync(Guid id, Guid leaseToken, string state, int status, byte[]? body, string? contentType, CancellationToken ct)
    {
        var now = IdempotencyRules.AsUtc(_utcNow(), "utcNow");
        var omitted = body is { Length: > 0 } && body.Length > _options.Limits.MaxResponseBytes;
        var stored = omitted ? null : body;
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE idempotency.entry
            SET state = @state,
                response_status = @status,
                response_body = @body,
                response_content_type = @content_type,
                response_omitted = @omitted,
                completed_timestamp = @completed
            WHERE id = @id AND lease_token = @token AND state = 'in_progress'
            """;
        cmd.Parameters.AddWithValue("state", state);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("body", (object?)stored ?? DBNull.Value);
        cmd.Parameters.AddWithValue("content_type", (object?)contentType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("omitted", omitted);
        cmd.Parameters.AddWithValue("completed", now);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("token", leaseToken);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 1;
    }

    private static async Task<IdempotencyEntry?> ReadLiveAsync(NpgsqlConnection conn, DbTransaction? transaction, string scope, string key, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = transaction as NpgsqlTransaction;
        cmd.CommandText = """
            SELECT id, kind, scope, key, request_hash, state, lease_token, response_status, response_body,
                   response_content_type, response_omitted, expires_at
            FROM idempotency.entry
            WHERE scope = @scope AND key = @key AND state IN ('in_progress', 'completed', 'failed')
            ORDER BY created_timestamp DESC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("scope", scope);
        cmd.Parameters.AddWithValue("key", key);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return new IdempotencyEntry {
            Id = reader.GetGuid(0),
            Kind = reader.GetString(1) == "outbox" ? IdempotencyKind.Outbox : IdempotencyKind.Api,
            Scope = reader.GetString(2),
            Key = reader.GetString(3),
            RequestHash = (byte[])reader.GetValue(4),
            State = ParseState(reader.GetString(5)),
            LeaseToken = reader.IsDBNull(6) ? null : reader.GetGuid(6),
            ResponseStatus = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            ResponseBody = reader.IsDBNull(8) ? null : (byte[])reader.GetValue(8),
            ResponseContentType = reader.IsDBNull(9) ? null : reader.GetString(9),
            ResponseOmitted = reader.GetBoolean(10),
            ExpiresAt = reader.IsDBNull(11) ? null : DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc)
        };
    }

    private static IdempotencyState ParseState(string state) => state switch {
        "in_progress" => IdempotencyState.InProgress,
        "completed" => IdempotencyState.Completed,
        "failed" => IdempotencyState.Failed,
        _ => IdempotencyState.Expired
    };

    private static IdempotencyEntry NewEntry(
        IdempotencyKind kind,
        string scope,
        string key,
        byte[] hash,
        DateTime? expiresAt,
        DateTime now,
        string? destination,
        byte[]? payload)
        => new() {
            Id = LyoGuid.CreateV7(),
            Kind = kind,
            Scope = scope,
            Key = key,
            RequestHash = hash,
            State = IdempotencyState.InProgress,
            LeaseToken = kind == IdempotencyKind.Api ? LyoGuid.CreateV7() : null,
            LeaseUntil = kind == IdempotencyKind.Api ? expiresAt : null,
            ExpiresAt = expiresAt,
            Destination = destination,
            Payload = payload,
            CreatedTimestamp = now
        };

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }

    private static NpgsqlConnection RequireConnection(DbTransaction transaction)
    {
        ArgumentHelpers.ThrowIfNull(transaction);
        if (transaction.Connection is not NpgsqlConnection conn)
            throw new ArgumentException("The transaction must be an open Npgsql transaction.", nameof(transaction));

        if (conn.State != ConnectionState.Open)
            throw new InvalidOperationException("The caller transaction's connection is not open.");

        return conn;
    }

    private static async Task ExecAsync(NpgsqlConnection conn, DbTransaction transaction, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = (NpgsqlTransaction)transaction;
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Result of inserting an outbox row into the shared idempotency table.</summary>
public sealed class IdempotencyOutboxInsert
{
    /// <summary>True when this call inserted the row.</summary>
    public bool Inserted { get; init; }

    /// <summary>Existing or new row id.</summary>
    public Guid Id { get; init; }

    /// <summary>How the claim resolved.</summary>
    public IdempotencyOutcome Outcome { get; init; }
}

/// <summary>A due outbox row whose lease was committed before publish.</summary>
public sealed class IdempotencyOutboxClaim
{
    /// <summary>Row id. Consumers dedupe on this value.</summary>
    public Guid Id { get; init; }

    /// <summary>Lease that must be presented to finish or retry the row.</summary>
    public Guid LeaseToken { get; init; }

    /// <summary>Queue name.</summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>Caller payload bytes.</summary>
    public byte[] Payload { get; set; } = [];

    /// <summary>Attempts already recorded before this claim.</summary>
    public int AttemptCount { get; init; }
}
