using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;

namespace Lyo.Authentication.Services.Users;

/// <summary>In-memory <see cref="IUserStore"/>. Used by default until <c>Lyo.Authentication.Postgres</c> overrides it. Thread-safe.</summary>
/// <remarks>The in-memory store does not enforce tenant filtering — the <c>tenantId</c> parameter is accepted for interface compatibility but ignored.</remarks>
public sealed class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<Guid, LyoUser> _byId = new();
    private readonly ConcurrentDictionary<string, Guid> _byEmail = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<LyoUser?> GetByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default) =>
        Task.FromResult<LyoUser?>(_byId.TryGetValue(id, out var u) ? u : null);

    /// <inheritdoc/>
    public Task<LyoUser?> GetByEmailAsync(string email, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(email);
        return Task.FromResult<LyoUser?>(_byEmail.TryGetValue(email, out var id) && _byId.TryGetValue(id, out var u) ? u : null);
    }

    /// <inheritdoc/>
    public Task<LyoUser> CreateAsync(LyoUser user, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(user);
        if (!_byEmail.TryAdd(user.Email, user.Id))
            throw new InvalidOperationException($"Email '{user.Email}' is already registered.");

        if (!_byId.TryAdd(user.Id, user)) {
            _byEmail.TryRemove(user.Email, out _);
            throw new InvalidOperationException($"User id '{user.Id}' already exists.");
        }

        return Task.FromResult(user);
    }

    /// <inheritdoc/>
    public Task UpdateLastLoginAsync(Guid id, DateTime utcNow, Guid? tenantId, CancellationToken ct = default)
    {
        _byId.AddOrUpdate(
            id, _ => throw new InvalidOperationException($"User id '{id}' not found."),
            (_, existing) => existing with { LastLoginAt = utcNow, UpdatedAt = utcNow });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetScopesAsync(Guid id, IReadOnlyList<string> scopes, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(scopes);
        var snap = scopes.ToArray();
        _byId.AddOrUpdate(
            id, _ => throw new InvalidOperationException($"User id '{id}' not found."),
            (_, existing) => existing with { Scopes = snap, UpdatedAt = DateTime.UtcNow });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetDisabledAsync(Guid id, DateTime? disabledAt, string? reason, Guid? tenantId, CancellationToken ct = default)
    {
        _byId.AddOrUpdate(
            id, _ => throw new InvalidOperationException($"User id '{id}' not found."),
            (_, existing) => existing with { DisabledAt = disabledAt, DisabledReason = reason, UpdatedAt = DateTime.UtcNow });

        return Task.CompletedTask;
    }
}
