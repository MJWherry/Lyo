using System.Diagnostics;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>Shared subject/actor endpoint columns on relation rows (maps to for_entity_* / from_entity_*).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class EntityRelationEndpointsEntityBase
{
    /// <summary>Type discriminator for the subject.</summary>
    public string? SubjectEntityType { get; set; }

    /// <summary>Id for <see cref="SubjectEntityType" />.</summary>
    public string? SubjectEntityId { get; set; }

    /// <summary>Type discriminator for the actor.</summary>
    public string? ActorEntityType { get; set; }

    /// <summary>Id for <see cref="ActorEntityType" />.</summary>
    public string? ActorEntityId { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{SubjectEntityType}:{SubjectEntityId} | {ActorEntityType}:{ActorEntityId}";
}