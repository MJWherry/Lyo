using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;

namespace Lyo.Authentication.Services.Users;

/// <summary>Persistence boundary for <see cref="LinkedIdentity"/>. Implemented in-memory for tests and by <c>Lyo.Authentication.Postgres</c> in production.</summary>
public interface IExternalIdentityStore
{
    /// <summary>Finds the active link for a (provider, subject) tuple. Returns <c>null</c> when no active link exists.</summary>
    Task<LinkedIdentity?> FindByProviderSubjectAsync(string provider, string subject, CancellationToken ct = default);

    /// <summary>Creates or refreshes a link for the given <paramref name="userId"/>.</summary>
    /// <remarks>
    /// If an active link already exists for (provider, subject) and it belongs to a different user, this throws. Otherwise the existing link's claims/email/scopes are refreshed and
    /// <c>UpdatedAt</c> and <c>LastUsedAt</c> are bumped. If no active link exists, a new one is created.
    /// </remarks>
    Task<LinkedIdentity> LinkAsync(
        Guid userId,
        string provider,
        string subject,
        string? emailAtLink,
        IReadOnlyList<string> scopes,
        IReadOnlyDictionary<string, object?>? rawClaims,
        CancellationToken ct = default);

    /// <summary>Lists all active links for a user.</summary>
    Task<IReadOnlyList<LinkedIdentity>> ListForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Soft-deletes a link (sets <c>UnlinkedAt</c>). The (provider, subject) pair can be relinked afterwards.</summary>
    Task UnlinkAsync(Guid linkedIdentityId, DateTime utcNow, CancellationToken ct = default);
}
