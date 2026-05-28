using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Services.Users;

/// <summary>Persistence boundary for <see cref="LyoUser" />. Implemented in-memory for tests and by <c>Lyo.Authentication.Postgres</c> in production.</summary>
/// <remarks>
/// Every method takes a <c>Guid? tenantId</c> that is resolved against the store's <c>TenancyOptions</c>: <c>SystemOnly</c> matches null-tenant rows,
/// <c>SingleTenantDefault</c> falls back to the configured default, and <c>MultiTenantStrict</c> requires a non-empty value.
/// </remarks>
public interface IUserStore
{
    /// <summary>Looks up a user by id, scoped to the resolved tenant. Returns <c>null</c> when not found.</summary>
    Task<LyoUser?> GetByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Looks up a user by email (case-insensitive), scoped to the resolved tenant. Returns <c>null</c> when not found.</summary>
    Task<LyoUser?> GetByEmailAsync(string email, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Inserts a new user stamped with the resolved tenant. Throws on duplicate <c>(tenant_id, email)</c> or duplicate id.</summary>
    Task<LyoUser> CreateAsync(LyoUser user, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Best-effort update of <see cref="LyoUser.LastLoginAt" />, scoped to the resolved tenant.</summary>
    Task UpdateLastLoginAsync(Guid id, DateTime utcNow, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Replaces <see cref="LyoUser.Scopes" />, scoped to the resolved tenant.</summary>
    Task SetScopesAsync(Guid id, IReadOnlyList<string> scopes, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Sets or clears <see cref="LyoUser.DisabledAt" />, scoped to the resolved tenant. When set, all existing tokens and JWTs for this user are rejected.</summary>
    Task SetDisabledAsync(Guid id, DateTime? disabledAt, string? reason, Guid? tenantId, CancellationToken ct = default);
}