using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;

namespace Lyo.Authentication.Services.Opaque;

/// <summary>Persistence boundary for <see cref="ApiTokenRecord"/>. Implemented in-memory for tests and by <c>Lyo.Authentication.Postgres</c> in production.</summary>
public interface IApiTokenStore
{
    /// <summary>Inserts a new record. Throws on duplicate <see cref="ApiTokenRecord.Id"/> so the issuer can retry with a fresh id.</summary>
    Task InsertAsync(ApiTokenRecord record, CancellationToken ct = default);

    /// <summary>Looks up a record by its 11-char id. Returns <c>null</c> when not found.</summary>
    Task<ApiTokenRecord?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Best-effort update of <see cref="ApiTokenRecord.LastUsedAt"/>. Failures are swallowed (called from the validation hot path).</summary>
    Task TouchLastUsedAsync(string id, DateTime utcNow, CancellationToken ct = default);

    /// <summary>Sets <see cref="ApiTokenRecord.RevokedAt"/> and <see cref="ApiTokenRecord.RevokedReason"/>.</summary>
    Task RevokeAsync(string id, DateTime revokedAt, string? reason, CancellationToken ct = default);

    /// <summary>Lists all tokens owned by <paramref name="userId"/>, oldest-first. Pass <c>includeRevoked = false</c> to omit dead tokens.</summary>
    Task<IReadOnlyList<ApiTokenRecord>> ListForUserAsync(Guid userId, bool includeRevoked, CancellationToken ct = default);
}
