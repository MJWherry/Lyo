using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Services.Users;

/// <summary>Persistence boundary for authorization scopes on a Lyo user. This store is the source of truth for JWT <c>scope</c> at issue time.</summary>
public interface IUserScopeStore
{
    /// <summary>Lists scopes for <paramref name="userId" />, oldest-first, scoped to the resolved tenant.</summary>
    Task<IReadOnlyList<LyoUserScope>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Inserts a scope. Duplicate <c>(UserId, Name)</c> throws <c>ConflictException</c>.</summary>
    Task<LyoUserScope> CreateAsync(LyoUserScope scope, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Deletes a scope by id. Missing ids throw <c>NotFoundException</c>.</summary>
    Task DeleteAsync(Guid id, Guid? tenantId, CancellationToken ct = default);
}
