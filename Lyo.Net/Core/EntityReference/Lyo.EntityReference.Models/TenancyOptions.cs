using System.Diagnostics;

namespace Lyo.EntityReference.Models;

/// <summary>Per-feature tenancy configuration. Each property is nullable so feature options can opt to inherit from <see cref="EntityRefOptions" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TenancyOptions
{
    /// <summary>Override the per-feature tenancy mode. When <see langword="null" /> the feature inherits <see cref="EntityRefOptions.Mode" />.</summary>
    public TenancyMode? Mode { get; set; }

    /// <summary>
    /// Override the per-feature default tenant id used by <see cref="TenancyMode.SingleTenantDefault" />. When <see langword="null" /> the feature inherits
    /// <see cref="EntityRefOptions.DefaultTenantId" />.
    /// </summary>
    public Guid? DefaultTenantId { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"TenancyOptions: Mode={Mode}, DefaultTenantId={DefaultTenantId}";
}