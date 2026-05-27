using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lyo.Authentication.OpenIdConnect.Handoff;

/// <summary>
/// Persistence contract for short-TTL, single-use <see cref="LyoHandoffCode"/> values. Implementations MUST enforce single-use semantics atomically: a concurrent
/// <see cref="ConsumeAsync"/> for the same id must return <c>null</c> for the loser. The contract is intentionally minimal so it can be backed by an in-memory cache for
/// single-instance hosts or a Redis/Postgres store for multi-instance deployments.
/// </summary>
public interface IHandoffCodeStore
{
    /// <summary>Stores <paramref name="code"/> with the supplied <paramref name="ttl"/>. After <paramref name="ttl"/> elapses, <see cref="ConsumeAsync"/> must return <c>null</c>.</summary>
    Task StoreAsync(LyoHandoffCode code, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Atomically consumes the code identified by <paramref name="id"/>. Returns the stored code only if (a) it exists, (b) it has not been previously consumed, (c) it has not
    /// expired, and (d) <paramref name="callerOrigin"/> matches <see cref="LyoHandoffCode.IssuedTo"/> (case-insensitive). Returns <c>null</c> on any failure.
    /// </summary>
    Task<LyoHandoffCode?> ConsumeAsync(string id, string callerOrigin, CancellationToken ct = default);
}
