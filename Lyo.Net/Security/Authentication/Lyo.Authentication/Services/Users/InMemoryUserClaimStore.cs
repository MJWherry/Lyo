using System.Collections.Concurrent;
using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Services.Users;

/// <summary>In-memory <see cref="IUserClaimStore" />. Tenant id is accepted for interface compatibility but ignored.</summary>
public sealed class InMemoryUserClaimStore : IUserClaimStore
{
    private readonly ConcurrentDictionary<Guid, LyoUserClaim> _claims = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<LyoUserClaim>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        var snapshot = _claims.Values.Where(c => c.UserId == userId).OrderBy(c => c.CreatedAt).ToArray();
        return Task.FromResult<IReadOnlyList<LyoUserClaim>>(snapshot);
    }

    /// <inheritdoc />
    public Task<LyoUserClaim> CreateAsync(LyoUserClaim claim, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(claim);
        if (!_claims.TryAdd(claim.Id, claim))
            throw new ConflictException($"Claim id '{claim.Id}' already exists.");

        return Task.FromResult(claim);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        if (!_claims.TryRemove(id, out var _))
            throw new NotFoundException($"Claim id '{id}' not found.");

        return Task.CompletedTask;
    }
}
