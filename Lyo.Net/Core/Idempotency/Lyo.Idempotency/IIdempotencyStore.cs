using System.Data.Common;
using Lyo.Health;

namespace Lyo.Idempotency;

/// <summary>
/// Claims a scope and key once. API rows are not stolen. A passed <see cref="DbTransaction" /> is only supported by the Postgres store;
/// the in-memory store throws so a rollback cannot be mistaken for a durable claim.
/// </summary>
public interface IIdempotencyStore : IHealth
{
    /// <summary>
    /// Claims <paramref name="scope" /> and <paramref name="key" /> for an API caller. A scope that starts with <c>outbox/</c> is rejected.
    /// When <paramref name="transaction" /> is null, an in-progress row is committed before <see cref="IdempotencyOutcome.Began" /> is returned.
    /// </summary>
    /// <param name="scope">Caller boundary. For HTTP this is <c>api|{subject}|{method}|{path}</c>.</param>
    /// <param name="key">Caller-supplied idempotency key.</param>
    /// <param name="requestHash">SHA-256 of the request. Use <see cref="IdempotencyHash" />.</param>
    /// <param name="expiresAt">When the claim stops replaying. Null uses the default lifetime. <see cref="DateTimeKind.Local" /> is rejected. Unspecified is UTC.</param>
    /// <param name="transaction">Caller transaction to join. Null opens a store-owned transaction.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    Task<IdempotencyBeginResult> TryBeginAsync(
        string scope,
        string key,
        byte[] requestHash,
        DateTime? expiresAt = null,
        DbTransaction? transaction = null,
        CancellationToken ct = default);

    /// <summary>Marks the row completed. A wrong <paramref name="leaseToken" /> leaves the row unchanged and returns false.</summary>
    Task<bool> CompleteAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default);

    /// <summary>Marks the row failed and stores the error the same way <see cref="CompleteAsync" /> stores a success. A wrong token returns false.</summary>
    Task<bool> FailAsync(Guid id, Guid leaseToken, int status, byte[]? body, string? contentType, CancellationToken ct = default);

    /// <summary>
    /// Expires a live API row so the key can be claimed again. Requires <paramref name="leaseToken" /> unless <paramref name="force" /> is set.
    /// Force may run the work twice.
    /// </summary>
    Task<bool> AbandonAsync(string scope, string key, Guid? leaseToken, bool force, CancellationToken ct = default);

    /// <summary>Expires API rows whose <c>expires_at</c> has passed, including stuck in-progress rows. Does not expire outbox rows.</summary>
    Task SweepExpiredAsync(CancellationToken ct = default);
}
