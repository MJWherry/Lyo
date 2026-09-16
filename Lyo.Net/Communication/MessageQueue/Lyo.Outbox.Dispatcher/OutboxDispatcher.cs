using System.Text.Json;
using Lyo.Idempotency.Postgres;
using Lyo.Json;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Outbox.Dispatcher;

/// <summary>
/// Claims due outbox rows, publishes them through <see cref="IMqService.SendToQueue" />, and finishes the row with the lease token.
/// Delivery is at-least-once. A crash after a successful send and before complete publishes again after the lease expires.
/// </summary>
public sealed class OutboxDispatcher : BackgroundService
{
    /// <summary>Fixed <see cref="IMetrics.RecordError" /> message.</summary>
    public const string StoreFailedMessage = "outbox.store_failed";

    private readonly OutboxDispatcherOptions _options;
    private readonly PostgresIdempotencyStore _store;
    private readonly IMqService _queue;
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;
    private readonly Func<DateTime> _utcNow;
    private readonly JsonSerializerOptions _json;

    /// <summary>Builds a dispatcher. <paramref name="utcNow" /> defaults to <see cref="DateTime.UtcNow" />.</summary>
    public OutboxDispatcher(
        OutboxDispatcherOptions options,
        PostgresIdempotencyStore store,
        IMqService queue,
        IMetrics? metrics = null,
        ILogger? logger = null,
        Func<DateTime>? utcNow = null)
    {
        Lyo.Exceptions.ArgumentHelpers.ThrowIfNull(options);
        Lyo.Exceptions.ArgumentHelpers.ThrowIfNull(store);
        Lyo.Exceptions.ArgumentHelpers.ThrowIfNull(queue);
        options.Validate();
        _options = options;
        _store = store;
        _queue = queue;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = logger ?? NullLogger.Instance;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
        _json = LyoJsonSerializerOptions.Create();
    }

    /// <summary>Claims one batch and publishes it. The host loop calls this on <see cref="OutboxDispatcherOptions.PollInterval" />.</summary>
    public async Task DispatchOnceAsync(CancellationToken ct = default)
    {
        IReadOnlyList<IdempotencyOutboxClaim> claims;
        try {
            claims = await _store.ClaimDueOutboxAsync(_options.BatchSize, _options.Lease, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            RecordStoreFailure(ex);
            throw;
        }

        foreach (var claim in claims) {
            ct.ThrowIfCancellationRequested();
            await PublishAsync(claim, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await DispatchOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
            catch (Exception ex) {
                _logger.LogDebug(ex, "Outbox poll failed");
            }

            await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task PublishAsync(IdempotencyOutboxClaim claim, CancellationToken ct)
    {
        using var _ = _metrics.StartTimer("outbox.publish");
        var wrapper = JsonSerializer.SerializeToUtf8Bytes(new OutboxDelivery { Id = claim.Id, Payload = claim.Payload }, _json);
        bool sent;
        try {
            sent = await _queue.SendToQueue(claim.Destination, wrapper).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "Outbox publish threw {Id}", claim.Id);
            await FailOrRetryAsync(claim, ct).ConfigureAwait(false);
            return;
        }

        if (sent) {
            try {
                var completed = await _store.CompleteAsync(claim.Id, claim.LeaseToken, 200, null, null, ct).ConfigureAwait(false);
                if (!completed) {
                    _logger.LogDebug("Outbox stale lease {Id}", claim.Id);
                    return;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException) {
                RecordStoreFailure(ex);
                throw;
            }

            _metrics.IncrementCounter("outbox.publish", tags: [("outcome", "sent")]);
            _logger.LogInformation("Outbox {Destination} {Outcome}", claim.Destination, "sent");
            _logger.LogDebug("Outbox {Id} {Destination}", claim.Id, claim.Destination);
            return;
        }

        try {
            await FailOrRetryAsync(claim, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            RecordStoreFailure(ex);
            throw;
        }
    }

    private async Task FailOrRetryAsync(IdempotencyOutboxClaim claim, CancellationToken ct)
    {
        var next = claim.AttemptCount + 1;
        if (next >= _options.MaxAttempts) {
            var failed = await _store.FailAsync(claim.Id, claim.LeaseToken, 500, null, null, ct).ConfigureAwait(false);
            if (!failed)
                return;

            _metrics.IncrementCounter("outbox.publish", tags: [("outcome", "poison")]);
            _logger.LogInformation("Outbox {Destination} {Outcome}", claim.Destination, "poison");
            _logger.LogDebug("Outbox {Id} {Destination}", claim.Id, claim.Destination);
            return;
        }

        var when = _utcNow().Add(_options.Backoff);
        var scheduled = await _store.ScheduleOutboxRetryAsync(claim.Id, claim.LeaseToken, when, ct).ConfigureAwait(false);
        if (!scheduled)
            return;

        _metrics.IncrementCounter("outbox.publish", tags: [("outcome", "retry")]);
        _logger.LogInformation("Outbox {Destination} {Outcome}", claim.Destination, "retry");
        _logger.LogDebug("Outbox {Id} {Destination}", claim.Id, claim.Destination);
    }

    private void RecordStoreFailure(Exception ex)
    {
        _metrics.RecordError("outbox.error", new InvalidOperationException(StoreFailedMessage));
        _logger.LogDebug(ex, "Outbox store failed");
    }
}
