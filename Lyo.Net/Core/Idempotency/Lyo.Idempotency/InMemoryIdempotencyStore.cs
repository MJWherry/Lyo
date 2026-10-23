using System.Data.Common;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Health;

namespace Lyo.Idempotency;

/// <summary>Process-local idempotency store. One lock covers claim, finish, and sweep. A <see cref="DbTransaction" /> is rejected: rollback safety exists only on the Postgres store.</summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly List<IdempotencyEntry> _entries = [];
    private readonly object _gate = new();
    private readonly IdempotencyOptions _options;
    private readonly Func<DateTime> _utcNow;

    /// <summary>Builds a store. <paramref name="utcNow" /> defaults to <see cref="DateTime.UtcNow" />.</summary>
    public InMemoryIdempotencyStore(IdempotencyOptions? options = null, Func<DateTime>? utcNow = null)
    {
        _options = options ?? new IdempotencyOptions();
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    /// <inheritdoc />
    public string HealthCheckName => "idempotency";

    /// <inheritdoc />
    public Task<IdempotencyBeginResult> TryBeginAsync(
        string scope,
        string key,
        byte[] requestHash,
        DateTime? expiresAt = null,
        DbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (transaction is not null)
            throw new NotSupportedException("The in-memory idempotency store cannot join a database transaction.");

        IdempotencyRules.ThrowIfApiScope(scope, key, requestHash, _options);
        var now = IdempotencyRules.AsUtc(_utcNow(), "utcNow");
        var expiry = IdempotencyRules.ResolveExpiry(expiresAt, now, _options.DefaultLifetime);
        lock (_gate) {
            var live = FindLive(scope, key);
            if (live is not null)
                return Task.FromResult(IdempotencyDecision.FromLive(live, requestHash, now));

            var entry = NewApiEntry(scope, key, requestHash, expiry, now);
            _entries.Add(entry);
            return Task.FromResult(IdempotencyDecision.Began(entry));
        }
    }

    /// <inheritdoc />
    public Task<bool> CompleteAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default)
        => Task.FromResult(Finish(id, leaseToken, IdempotencyState.Completed, status, body, contentType, ct));

    /// <inheritdoc />
    public Task<bool> FailAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default)
        => Task.FromResult(Finish(id, leaseToken, IdempotencyState.Failed, status, body, contentType, ct));

    /// <inheritdoc />
    public Task<bool> AbandonAsync(string scope, string key, Guid? leaseToken, bool force, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scope);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        lock (_gate) {
            var live = FindLive(scope, key);
            if (live is null || live.Kind != IdempotencyKind.Api)
                return Task.FromResult(false);

            if (!force && live.LeaseToken != leaseToken)
                return Task.FromResult(false);

            live.State = IdempotencyState.Expired;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task SweepExpiredAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var now = IdempotencyRules.AsUtc(_utcNow(), "utcNow");
        lock (_gate) {
            foreach (var entry in _entries) {
                if (entry.Kind != IdempotencyKind.Api || !entry.IsLive)
                    continue;

                if (IdempotencyRules.IsTimeExpired(entry.ExpiresAt, now))
                    entry.State = IdempotencyState.Expired;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(HealthResult.Healthy(TimeSpan.Zero));
    }

    private bool Finish(Guid id, Guid leaseToken, IdempotencyState state, int status, byte[]? body, string? contentType, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var now = IdempotencyRules.AsUtc(_utcNow(), "utcNow");
        lock (_gate) {
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (entry is null || entry.State != IdempotencyState.InProgress || entry.LeaseToken != leaseToken)
                return false;

            ApplyBody(entry, status, body, contentType);
            entry.State = state;
            entry.CompletedTimestamp = now;
            return true;
        }
    }

    private void ApplyBody(IdempotencyEntry entry, int status, byte[]? body, string? contentType)
    {
        entry.ResponseStatus = status;
        entry.ResponseContentType = contentType;
        if (body is { Length: > 0 } && body.Length > _options.MaxResponseBytes) {
            entry.ResponseBody = null;
            entry.ResponseOmitted = true;
            return;
        }

        entry.ResponseBody = body;
        entry.ResponseOmitted = false;
    }

    private IdempotencyEntry? FindLive(string scope, string key) => _entries.FirstOrDefault(e => e.Scope == scope && e.Key == key && e.IsLive);

    private IdempotencyEntry NewApiEntry(string scope, string key, byte[] requestHash, DateTime expiry, DateTime now)
        => new() {
            Id = LyoGuid.CreateV7(),
            Kind = IdempotencyKind.Api,
            Scope = scope,
            Key = key,
            RequestHash = requestHash.ToArray(),
            State = IdempotencyState.InProgress,
            LeaseToken = LyoGuid.CreateV7(),
            LeaseUntil = expiry,
            ExpiresAt = expiry,
            CreatedTimestamp = now,
            AttemptCount = 0
        };
}