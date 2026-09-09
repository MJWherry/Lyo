using System.Collections.Concurrent;
using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Services.Users;

/// <summary>In-memory <see cref="IUserScopeStore" />. Tenant id is accepted for interface compatibility but ignored.</summary>
public sealed class InMemoryUserScopeStore : IUserScopeStore
{
    private readonly ConcurrentDictionary<Guid, LyoUserScope> _scopes = new();

    /// <inheritdoc />
    public Task<IReadOnlyList<LyoUserScope>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        var snapshot = _scopes.Values.Where(s => s.UserId == userId).OrderBy(s => s.CreatedAt).ToArray();
        return Task.FromResult<IReadOnlyList<LyoUserScope>>(snapshot);
    }

    /// <inheritdoc />
    public Task<LyoUserScope> CreateAsync(LyoUserScope scope, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(scope);
        if (_scopes.Values.Any(s => s.UserId == scope.UserId && string.Equals(s.Name, scope.Name, StringComparison.Ordinal)))
            throw new ConflictException($"Scope '{scope.Name}' is already granted to user '{scope.UserId}'.");

        if (!_scopes.TryAdd(scope.Id, scope))
            throw new ConflictException($"Scope id '{scope.Id}' already exists.");

        return Task.FromResult(scope);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        if (!_scopes.TryRemove(id, out var _))
            throw new NotFoundException($"Scope id '{id}' not found.");

        return Task.CompletedTask;
    }
}
