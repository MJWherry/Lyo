using System.Diagnostics;

namespace Lyo.EntityReference.Models;

/// <summary>Host configuration for association stores (single-tenant default tenant, default tenancy mode).</summary>
[DebuggerDisplay("Mode={Mode}, DefaultTenantId={DefaultTenantId}")]
public sealed class EntityRefOptions
{
    /// <summary>Tenant identifier applied when store methods receive <see langword="null" /> or omit tenant context (single-tenant deployments).</summary>
    /// <remarks>Defaults to <see cref="EntityRefWellKnown.SingleTenantDefaultId" />.</remarks>
    public Guid DefaultTenantId { get; set; } = EntityRefWellKnown.SingleTenantDefaultId;

    /// <summary>App-wide default <see cref="TenancyMode" /> applied when a feature's <see cref="TenancyOptions.Mode" /> is unset.</summary>
    /// <remarks>Defaults to <see cref="TenancyMode.SingleTenantDefault" /> for out-of-the-box single-deployment ergonomics.</remarks>
    public TenancyMode Mode { get; set; } = TenancyMode.SingleTenantDefault;

    /// <inheritdoc />
    public override string ToString() => $"EntityRefOptions: Mode={Mode}, DefaultTenantId={DefaultTenantId}";
}