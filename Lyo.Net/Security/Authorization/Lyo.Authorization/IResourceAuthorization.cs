namespace Lyo.Authorization;

/// <summary>Saves and revokes grants with audit, and answers a check. Checks are not audited.</summary>
public interface IResourceAuthorization
{
    /// <summary>Saves a grant. An in-force duplicate throws <see cref="Lyo.Exceptions.Models.ConflictException" /> and is counted as a conflict, not a store error.</summary>
    Task<Grant> SaveAsync(GrantWrite write, CancellationToken ct = default);

    /// <summary>Revokes a grant. A missing id throws <see cref="Lyo.Exceptions.NotFoundException" />. A second revoke returns the row.</summary>
    Task<Grant> RevokeAsync(Guid id, CancellationToken ct = default);

    /// <summary>True when the store has a matching grant. A store exception propagates and is not counted as a deny.</summary>
    Task<bool> IsAllowedAsync(GrantCheck check, CancellationToken ct = default);
}
