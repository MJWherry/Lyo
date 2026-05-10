using System.Diagnostics;

namespace Lyo.EntityReference.Models;

/// <summary>Host configuration for association stores (single-tenant default tenant, etc.).</summary>
[DebuggerDisplay("DefaultTenantId={DefaultTenantId}")]
public sealed class EntityRefOptions
{
    /// <summary>
    /// Tenant identifier applied when store methods receive <see langword="null"/> or omit tenant context (single-tenant deployments).
    /// </summary>
    /// <remarks>Defaults to <see cref="EntityRefWellKnown.SingleTenantDefaultId"/>.</remarks>
    public Guid DefaultTenantId { get; set; } = EntityRefWellKnown.SingleTenantDefaultId;

    /// <inheritdoc />
    public override string ToString() => $"EntityRefOptions: DefaultTenantId={DefaultTenantId}";
}
