using Lyo.Authentication.Models.Records;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;

namespace Lyo.Authentication.Services.Users;

/// <summary>Resolves and mirrors internal user scopes used for JWT/PAT issuance. Provider/link scopes are never consulted.</summary>
public static class UserScopeNames
{
    /// <summary>Prefers store rows. An empty or missing store returns no scopes.</summary>
    public static async Task<IReadOnlyList<string>> ResolveAsync(IUserScopeStore? store, LyoUser user, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(user);
        if (store is null)
            return [];

        var rows = await store.ListForUserAsync(user.Id, null, ct).ConfigureAwait(false);
        return DistinctNames(rows.Select(r => r.Name));
    }

    /// <summary>Inserts missing names for <paramref name="userId" />. Existing names are left alone.</summary>
    public static async Task SeedAsync(IUserScopeStore store, Guid userId, IReadOnlyList<string> names, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNull(names);
        var existing = new HashSet<string>((await store.ListForUserAsync(userId, tenantId, ct).ConfigureAwait(false)).Select(s => s.Name), StringComparer.Ordinal);
        foreach (var name in names) {
            if (string.IsNullOrWhiteSpace(name) || !existing.Add(name))
                continue;

            var now = DateTime.UtcNow;
            await store.CreateAsync(new(LyoGuid.CreateV7(), userId, name, now, null), tenantId, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Copies store names onto <c>UserEntity.ScopesJson</c> so validators that still read <see cref="LyoUser.Scopes" /> stay in sync.</summary>
    public static async Task MirrorToUserAsync(IUserStore users, IUserScopeStore store, Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(users);
        ArgumentHelpers.ThrowIfNull(store);
        var names = DistinctNames((await store.ListForUserAsync(userId, tenantId, ct).ConfigureAwait(false)).Select(s => s.Name));
        await users.SetScopesAsync(userId, names, tenantId, ct).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> DistinctNames(IEnumerable<string> names) => names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal).ToArray();
}