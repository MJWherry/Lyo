using Lyo.EntityReference.Models;
using Lyo.Exceptions;

namespace Lyo.EntityReference.Postgres;

/// <summary>Resolves a nullable caller tenant id under a feature or global <see cref="TenancyMode" /> policy.</summary>
public static class TenancyResolver
{
    /// <summary>
    /// Resolves the effective tenant id (or <see langword="null" /> for system rows) using per-feature options, falling back to the global host options.
    /// </summary>
    /// <param name="tenantId">Caller-supplied tenant. <see cref="Guid.Empty" /> is treated the same as <see langword="null" />.</param>
    /// <param name="feature">Per-feature tenancy block. Each unset property inherits from <paramref name="global" />.</param>
    /// <param name="global">Global host options providing the fallback <see cref="EntityRefOptions.Mode" /> and <see cref="EntityRefOptions.DefaultTenantId" />.</param>
    /// <returns>Resolved tenant id, or <see langword="null" /> when the effective mode is <see cref="TenancyMode.SystemOnly" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="feature" /> or <paramref name="global" /> is null, or the effective mode is <see cref="TenancyMode.MultiTenantStrict" />
    /// and the caller did not supply a non-empty tenant.
    /// </exception>
    public static Guid? Resolve(Guid? tenantId, TenancyOptions feature, EntityRefOptions global)
    {
        ArgumentHelpers.ThrowIfNull(feature);
        ArgumentHelpers.ThrowIfNull(global);
        var mode = feature.Mode ?? global.Mode;
        var hasCaller = tenantId is { } t && t != Guid.Empty;
        return mode switch {
            TenancyMode.SystemOnly => null,
            TenancyMode.MultiTenantStrict when !hasCaller => throw new ArgumentNullException(nameof(tenantId), "TenancyMode.MultiTenantStrict requires a non-empty tenantId."),
            TenancyMode.MultiTenantStrict => tenantId,
            TenancyMode.MultiTenantOptional when hasCaller => tenantId,
            TenancyMode.MultiTenantOptional => null,
            TenancyMode.SingleTenantDefault when hasCaller => tenantId,
            TenancyMode.SingleTenantDefault => feature.DefaultTenantId ?? global.DefaultTenantId,
            var _ => throw new ArgumentOutOfRangeException(nameof(feature), mode, $"Unsupported {nameof(TenancyMode)} value '{mode}'.")
        };
    }
}