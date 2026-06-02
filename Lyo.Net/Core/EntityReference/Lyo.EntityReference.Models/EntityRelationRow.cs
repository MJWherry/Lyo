using System.Diagnostics;

namespace Lyo.EntityReference.Models;

/// <summary>Domain mirror of a persisted relation row (subject/actor, tenant, lifecycle).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class EntityRelationRow
{
    /// <summary>Row primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Type discriminator for the subject entity.</summary>
    public string? SubjectEntityType { get; set; }

    /// <summary>Entity id for <see cref="SubjectEntityType" />.</summary>
    public string? SubjectEntityId { get; set; }

    /// <summary>Type discriminator for the actor entity.</summary>
    public string? ActorEntityType { get; set; }

    /// <summary>Entity id for <see cref="ActorEntityType" />.</summary>
    public string? ActorEntityId { get; set; }

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

    /// <summary>Visibility label (for example <see cref="EntityRefVisibility.Private" />).</summary>
    public string Visibility { get; set; } = EntityRefVisibility.Private;

    /// <summary>Gets the entity reference for the subject of this row.</summary>
    public EntityRef SubjectRef => EntityRef.ForKey(SubjectEntityType ?? string.Empty, SubjectEntityId ?? string.Empty);

    /// <summary>Gets the entity reference for the actor of this row.</summary>
    public EntityRef ActorRef => EntityRef.ForKey(ActorEntityType ?? string.Empty, ActorEntityId ?? string.Empty);

    /// <inheritdoc />
    public override string ToString()
        => $"{GetType().Name}: Id={Id}, Tenant={TenantId}, Subject={SubjectEntityType}/{SubjectEntityId}, Actor={ActorEntityType}/{ActorEntityId}, Visibility={Visibility}, DeletedAt={DeletedAt:O}";
}