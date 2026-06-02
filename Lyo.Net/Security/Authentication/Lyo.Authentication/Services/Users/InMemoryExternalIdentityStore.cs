using System.Collections.Concurrent;
using Lyo.Authentication.Models.Records;
using Lyo.Exceptions;

namespace Lyo.Authentication.Services.Users;

/// <summary>In-memory <see cref="IExternalIdentityStore" />. Used by default until <c>Lyo.Authentication.Postgres</c> overrides it. Thread-safe.</summary>
/// <remarks>The in-memory store does not enforce tenant filtering — the <c>tenantId</c> parameter is accepted for interface compatibility but ignored.</remarks>
public sealed class InMemoryExternalIdentityStore : IExternalIdentityStore
{
    private readonly ConcurrentDictionary<Guid, LinkedIdentity> _byId = new();

    /// <inheritdoc />
    public Task<LinkedIdentity?> FindByProviderSubjectAsync(string provider, string subject, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(subject);
        var match = _byId.Values.FirstOrDefault(l
            => l.IsActive && string.Equals(l.Provider, provider, StringComparison.Ordinal) && string.Equals(l.Subject, subject, StringComparison.Ordinal));

        return Task.FromResult(match);
    }

    /// <inheritdoc />
    public Task<LinkedIdentity> LinkAsync(
        Guid userId,
        string provider,
        string subject,
        string? emailAtLink,
        IReadOnlyList<string> scopes,
        IReadOnlyDictionary<string, object?>? rawClaims,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(subject);
        ArgumentHelpers.ThrowIfNull(scopes);
        var now = DateTime.UtcNow;
        var snapScopes = scopes.ToArray();
        var existing = _byId.Values.FirstOrDefault(l
            => l.IsActive && string.Equals(l.Provider, provider, StringComparison.Ordinal) && string.Equals(l.Subject, subject, StringComparison.Ordinal));

        if (existing is not null) {
            if (existing.UserId != userId)
                throw new InvalidOperationException($"({provider}, {subject}) is already linked to a different Lyo user.");

            var updated = existing with {
                EmailAtLink = emailAtLink ?? existing.EmailAtLink,
                Scopes = snapScopes,
                RawClaims = rawClaims,
                UpdatedAt = now,
                LastUsedAt = now
            };

            _byId[existing.Id] = updated;
            return Task.FromResult(updated);
        }

        var link = new LinkedIdentity(Guid.NewGuid(), userId, provider, subject, emailAtLink, snapScopes, rawClaims, now, now, now, null);
        _byId[link.Id] = link;
        return Task.FromResult(link);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LinkedIdentity>> ListForUserAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<LinkedIdentity> snapshot = _byId.Values.Where(l => l.UserId == userId && l.IsActive).OrderBy(l => l.LinkedAt).ToArray();
        return Task.FromResult(snapshot);
    }

    /// <inheritdoc />
    public Task UnlinkAsync(Guid linkedIdentityId, DateTime utcNow, Guid? tenantId, CancellationToken ct = default)
    {
        _byId.AddOrUpdate(
            linkedIdentityId, _ => throw new InvalidOperationException($"Linked identity '{linkedIdentityId}' not found."),
            (_, existing) => existing with { UnlinkedAt = utcNow, UpdatedAt = utcNow });

        return Task.CompletedTask;
    }
}