using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Services.Users;

/// <summary>Persistence boundary for extra JWT claims on a Lyo user.</summary>
public interface IUserClaimStore
{
    /// <summary>Lists claims for <paramref name="userId" />, oldest-first, scoped to the resolved tenant.</summary>
    Task<IReadOnlyList<LyoUserClaim>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Inserts a claim. Callers must not use reserved claim types; the JWT issuer skips them if they slip through.</summary>
    Task<LyoUserClaim> CreateAsync(LyoUserClaim claim, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Deletes a claim by id. Missing ids throw <c>NotFoundException</c>.</summary>
    Task DeleteAsync(Guid id, Guid? tenantId, CancellationToken ct = default);
}
