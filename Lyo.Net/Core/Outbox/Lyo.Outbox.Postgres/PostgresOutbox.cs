using System.Data.Common;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Idempotency.Postgres;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Outbox.Postgres;

/// <summary>
/// Writes outbox rows through <see cref="PostgresIdempotencyStore" /> on the caller's transaction. Does not commit.
/// A different payload is a conflict: the store rolls the transaction back, then this type rethrows.
/// </summary>
public sealed class PostgresOutbox : IOutbox
{
    /// <summary>Fixed <see cref="IMetrics.RecordError" /> message.</summary>
    public const string StoreFailedMessage = "outbox.store_failed";

    private readonly PostgresIdempotencyStore _store;
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;

    /// <summary>Builds an outbox over the Postgres idempotency store.</summary>
    public PostgresOutbox(PostgresIdempotencyStore store, IMetrics? metrics = null, ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(store);
        _store = store;
        _metrics = metrics ?? NullMetrics.Instance;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string HealthCheckName => "outbox";

    /// <inheritdoc />
    public async Task<OutboxEnqueueResult> EnqueueAsync(DbTransaction transaction, OutboxMessage message, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(transaction);
        ArgumentHelpers.ThrowIfNull(message);
        try {
            var inserted = await _store.InsertOutboxAsync(transaction, message.Destination, message.CallerScope, message.Key, message.Payload, ct).ConfigureAwait(false);
            var outcome = inserted.Inserted ? "inserted" : "duplicate";
            _metrics.IncrementCounter("outbox.enqueue", tags: [("outcome", outcome)]);
            _logger.LogInformation("Outbox {Destination} {Outcome}", message.Destination, outcome);
            _logger.LogDebug("Outbox {Id} {Destination}", inserted.Id, message.Destination);
            return new OutboxEnqueueResult { Inserted = inserted.Inserted, Id = inserted.Id };
        }
        catch (ConflictException) {
            _logger.LogInformation("Outbox {Destination} {Outcome}", message.Destination, "conflict");
            throw;
        }
        catch (Exception ex) {
            _metrics.RecordError("outbox.error", new InvalidOperationException(StoreFailedMessage));
            _logger.LogDebug(ex, "Outbox store failed for {Destination}", message.Destination);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> ReplayFailedAsync(Guid id, CancellationToken ct = default) => _store.ReplayFailedOutboxAsync(id, ct);

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => _store.CheckHealthAsync(ct);
}
