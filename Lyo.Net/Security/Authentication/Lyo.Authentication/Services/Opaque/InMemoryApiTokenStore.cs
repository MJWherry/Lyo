using System.Collections.Concurrent;
using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;

namespace Lyo.Authentication.Services.Opaque;

/// <summary>In-memory <see cref="IApiTokenStore" />. Used by default until <c>Lyo.Authentication.Postgres</c> overrides it. Thread-safe.</summary>
/// <remarks>The in-memory store does not enforce tenant filtering — the <c>tenantId</c> parameter is accepted for interface compatibility but ignored.</remarks>
public sealed class InMemoryApiTokenStore : IApiTokenStore
{
    private readonly ConcurrentDictionary<string, ApiTokenRecord> _records = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task InsertAsync(ApiTokenRecord record, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(record);
        if (!_records.TryAdd(record.Id, record))
            throw new InvalidOperationException($"Token id '{record.Id}' already exists.");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ApiTokenRecord?> GetByIdAsync(string id, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        return Task.FromResult<ApiTokenRecord?>(_records.TryGetValue(id, out var r) ? r : null);
    }

    /// <inheritdoc />
    public Task TouchLastUsedAsync(string id, DateTime utcNow, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        _records.AddOrUpdate(
            id, _ => throw new InvalidOperationException($"Token id '{id}' not found."), (_, existing) => existing with { LastUsedAt = utcNow, UpdatedAt = utcNow });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RevokeAsync(string id, DateTime revokedAt, string? reason, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        _records.AddOrUpdate(
            id, _ => throw new InvalidOperationException($"Token id '{id}' not found."),
            (_, existing) => existing with { RevokedAt = revokedAt, RevokedReason = reason, UpdatedAt = revokedAt });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ApiTokenRecord>> ListForUserAsync(Guid userId, bool includeRevoked, Guid? tenantId, CancellationToken ct = default)
    {
        var snapshot = _records.Values.Where(r => r.UserId == userId).Where(r => includeRevoked || !r.RevokedAt.HasValue).OrderBy(r => r.CreatedAt).ToArray();
        return Task.FromResult<IReadOnlyList<ApiTokenRecord>>(snapshot);
    }
}