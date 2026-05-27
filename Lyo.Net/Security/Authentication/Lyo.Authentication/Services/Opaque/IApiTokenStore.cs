using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;

namespace Lyo.Authentication.Services.Opaque;

/// <summary>Persistence boundary for <see cref="ApiTokenRecord"/>. Implemented in-memory for tests and by <c>Lyo.Authentication.Postgres</c> in production.</summary>
/// <remarks>Every method takes a <c>Guid? tenantId</c> resolved against the store's <c>TenancyOptions</c>: <c>SystemOnly</c> matches null-tenant rows, <c>SingleTenantDefault</c> falls back to the configured default, and <c>MultiTenantStrict</c> requires a non-empty value.</remarks>
public interface IApiTokenStore
{
    /// <summary>Inserts a new record stamped with the resolved tenant. Throws on duplicate <see cref="ApiTokenRecord.Id"/> so the issuer can retry with a fresh id.</summary>
    Task InsertAsync(ApiTokenRecord record, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Looks up a record by its 11-char id, scoped to the resolved tenant. Returns <c>null</c> when not found.</summary>
    Task<ApiTokenRecord?> GetByIdAsync(string id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Best-effort update of <see cref="ApiTokenRecord.LastUsedAt"/>, scoped to the resolved tenant. Failures are swallowed (called from the validation hot path).</summary>
    Task TouchLastUsedAsync(string id, DateTime utcNow, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Sets <see cref="ApiTokenRecord.RevokedAt"/> and <see cref="ApiTokenRecord.RevokedReason"/>, scoped to the resolved tenant.</summary>
    Task RevokeAsync(string id, DateTime revokedAt, string? reason, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Lists all tokens owned by <paramref name="userId"/>, oldest-first, scoped to the resolved tenant. Pass <c>includeRevoked = false</c> to omit dead tokens.</summary>
    Task<IReadOnlyList<ApiTokenRecord>> ListForUserAsync(Guid userId, bool includeRevoked, Guid? tenantId, CancellationToken ct = default);
}
