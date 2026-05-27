using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;

namespace Lyo.Authentication.Services.Users;

/// <summary>Persistence boundary for <see cref="LyoUser"/>. Implemented in-memory for tests and by <c>Lyo.Authentication.Postgres</c> in production.</summary>
public interface IUserStore
{
    /// <summary>Looks up a user by id. Returns <c>null</c> when not found.</summary>
    Task<LyoUser?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Looks up a user by email (case-insensitive). Returns <c>null</c> when not found.</summary>
    Task<LyoUser?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Inserts a new user. Throws on duplicate email or id.</summary>
    Task<LyoUser> CreateAsync(LyoUser user, CancellationToken ct = default);

    /// <summary>Best-effort update of <see cref="LyoUser.LastLoginAt"/>.</summary>
    Task UpdateLastLoginAsync(Guid id, DateTime utcNow, CancellationToken ct = default);

    /// <summary>Replaces <see cref="LyoUser.Scopes"/>. Future tokens minted will see the new set; existing tokens are unaffected unless <see cref="Options.AuthenticationOptions.EnableDynamicScopeIntersection"/> is on.</summary>
    Task SetScopesAsync(Guid id, IReadOnlyList<string> scopes, CancellationToken ct = default);

    /// <summary>Sets or clears <see cref="LyoUser.DisabledAt"/>. When set, all existing tokens and JWTs for this user are rejected.</summary>
    Task SetDisabledAsync(Guid id, DateTime? disabledAt, string? reason, CancellationToken ct = default);
}
