using Lyo.Health;

namespace Lyo.Authorization;

/// <summary>Persists grants. Implementations are singletons. A Postgres store opens a context per call.</summary>
public interface IGrantStore : IHealth
{
    /// <summary>Inserts a grant. An in-force duplicate throws <see cref="Lyo.Exceptions.Models.ConflictException" />. An expired duplicate is revoked and replaced.</summary>
    Task<Grant> SaveAsync(GrantWrite write, CancellationToken ct = default);

    /// <summary>Revokes a grant. A missing id throws <see cref="Lyo.Exceptions.Models.NotFoundException" />. A second revoke returns the row.</summary>
    Task<Grant> RevokeAsync(Guid id, CancellationToken ct = default);

    /// <summary>True when any stored grant matches <paramref name="check" />.</summary>
    Task<bool> IsAllowedAsync(GrantCheck check, CancellationToken ct = default);

    /// <summary>Lists grants. The default hides revoked and expired rows.</summary>
    Task<IReadOnlyList<Grant>> ListAsync(GrantListQuery? query = null, CancellationToken ct = default);
}