using System.Diagnostics;

namespace Lyo.EntityReference.Models;

/// <summary>Domain mirror of the canonical persisted association row (for/from entities, tenant, lifecycle).</summary>
/// <remarks>Pair with <see cref="EntityRef"/> for API boundaries; this type is the persisted row shape.</remarks>
[DebuggerDisplay("{ForEntityType,nq}:{ForEntityId} | {FromEntityType,nq}:{FromEntityId} | Tenant={TenantId}")]
public abstract class EntityRefRow
{
    /// <summary>Row primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Type discriminator for the entity being referenced (what the association applies to).</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Entity id for <see cref="ForEntityType"/> (single <see cref="Guid"/> per Option A persistence).</summary>
    public Guid ForEntityId { get; set; }

    /// <summary>Type discriminator for the originating actor entity.</summary>
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Entity id for <see cref="FromEntityType"/>.</summary>
    public Guid FromEntityId { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Optional workspace / project / personal scope label.</summary>
    public string? Context { get; set; }

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Optional expiry (UTC).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Soft-delete timestamp (UTC), if deleted.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Type of actor that performed soft-delete.</summary>
    public string? DeletedByType { get; set; }

    /// <summary>Id of actor that performed soft-delete.</summary>
    public Guid? DeletedById { get; set; }

    /// <summary>JSON payload for module-specific metadata (serialized form).</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Visibility label (for example <see cref="EntityRefVisibility.Private"/>).</summary>
    public string Visibility { get; set; } = EntityRefVisibility.Private;

    /// <inheritdoc />
    public override string ToString() =>
        $"{GetType().Name}: Id={Id}, Tenant={TenantId}, For={ForEntityType}/{ForEntityId}, From={FromEntityType}/{FromEntityId}, Visibility={Visibility}, DeletedAt={DeletedAt:O}";
}
