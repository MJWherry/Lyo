using System.Data.Common;
using Lyo.Health;

namespace Lyo.Outbox;

/// <summary>
/// Records an outbox row on a caller transaction and does not commit it. Delivery is at-least-once: a crash after a successful send and before
/// the row is completed publishes again after the lease expires.
/// </summary>
public interface IOutbox : IHealth
{
    /// <summary>
    /// Inserts <paramref name="message" /> on <paramref name="transaction" />. Does not commit.
    /// The same payload returns <see cref="OutboxEnqueueResult.Inserted" /> false and leaves the transaction open.
    /// A different payload rolls the transaction back and throws <see cref="Lyo.Exceptions.Models.ConflictException" />.
    /// </summary>
    Task<OutboxEnqueueResult> EnqueueAsync(DbTransaction transaction, OutboxMessage message, CancellationToken ct = default);

    /// <summary>Returns a failed outbox row to in-progress with attempts reset. API rows and rows that are not failed are unchanged.</summary>
    Task<bool> ReplayFailedAsync(Guid id, CancellationToken ct = default);
}
