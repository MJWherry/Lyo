using System.Data.Common;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Idempotency;

/// <summary>
/// Counts outcomes and records store failures with a fixed error message. <see cref="IMetrics.RecordError" /> copies <see cref="Exception.Message" /> into a tag, so the
/// provider exception is not passed through.
/// </summary>
public sealed class ObservingIdempotencyStore : IIdempotencyStore
{
    /// <summary>Fixed <see cref="IMetrics.RecordError" /> message. Must not contain a key or subject.</summary>
    public const string StoreFailedMessage = "idempotency.store_failed";

    private readonly IIdempotencyStore _inner;
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;

    /// <summary>Wraps <paramref name="inner" />. Missing metrics and logger become no-ops.</summary>
    public ObservingIdempotencyStore(IIdempotencyStore inner, IMetrics? metrics = null, ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(inner);
        _inner = inner;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string HealthCheckName => _inner.HealthCheckName;

    /// <inheritdoc />
    public Task<IdempotencyBeginResult> TryBeginAsync(
        string scope,
        string key,
        byte[] requestHash,
        DateTime? expiresAt = null,
        DbTransaction? transaction = null,
        CancellationToken ct = default)
        => Observe(
            "idempotency.begin", scope, () => _inner.TryBeginAsync(scope, key, requestHash, expiresAt, transaction, ct), result => {
                _metrics.IncrementCounter("idempotency.begin", tags: [("outcome", OutcomeTag(result.Outcome))]);
                _logger.LogInformation("Idempotency {Outcome}", OutcomeTag(result.Outcome));
                _logger.LogDebug("Idempotency {Outcome} {Scope}", OutcomeTag(result.Outcome), scope);
            });

    /// <inheritdoc />
    public Task<bool> CompleteAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default)
        => ObserveFlag("idempotency.complete", () => _inner.CompleteAsync(id, leaseToken, status, body, contentType, ct));

    /// <inheritdoc />
    public Task<bool> FailAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default)
        => ObserveFlag("idempotency.fail", () => _inner.FailAsync(id, leaseToken, status, body, contentType, ct));

    /// <inheritdoc />
    public Task<bool> AbandonAsync(string scope, string key, Guid? leaseToken, bool force, CancellationToken ct = default)
        => ObserveFlag("idempotency.abandon", () => _inner.AbandonAsync(scope, key, leaseToken, force, ct));

    /// <inheritdoc />
    public Task SweepExpiredAsync(CancellationToken ct = default) => Guard(() => _inner.SweepExpiredAsync(ct));

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => _inner.CheckHealthAsync(ct);

    private async Task<IdempotencyBeginResult> Observe(string timer, string scope, Func<Task<IdempotencyBeginResult>> call, Action<IdempotencyBeginResult> onSuccess)
    {
        using var _ = _metrics.StartTimer(timer);
        try {
            var result = await call().ConfigureAwait(false);
            onSuccess(result);
            return result;
        }
        catch (Exception ex) {
            RecordFailure(ex, scope);
            throw;
        }
    }

    private async Task<bool> ObserveFlag(string name, Func<Task<bool>> call)
    {
        try {
            var ok = await call().ConfigureAwait(false);
            if (ok)
                _metrics.IncrementCounter(name);

            return ok;
        }
        catch (Exception ex) {
            RecordFailure(ex, null);
            throw;
        }
    }

    private async Task Guard(Func<Task> call)
    {
        try {
            await call().ConfigureAwait(false);
        }
        catch (Exception ex) {
            RecordFailure(ex, null);
            throw;
        }
    }

    private void RecordFailure(Exception ex, string? scope)
    {
        _metrics.RecordError("idempotency.error", new InvalidOperationException(StoreFailedMessage));
        if (scope is null)
            _logger.LogDebug(ex, "Idempotency store failed");
        else
            _logger.LogDebug(ex, "Idempotency store failed for {Scope}", scope);
    }

    private static string OutcomeTag(IdempotencyOutcome outcome)
        => outcome switch {
            IdempotencyOutcome.Began => "began",
            IdempotencyOutcome.Replay => "replay",
            IdempotencyOutcome.InProgress => "in_progress",
            var _ => "conflict"
        };
}