using System.Diagnostics;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>
/// EF columns for a string-keyed target entity plus optional &quot;from&quot; actor (for example change history).
/// This is not the Option A uuid-backed <see cref="EntityRefEntityBase"/> row; use that for tenant-scoped associations with GUID ids.
/// </summary>
[DebuggerDisplay("{ForEntityType,nq}:{ForEntityId,nq} | From={FromEntityType,nq}:{FromEntityId,nq}")]
public abstract class EntityRefOptionalFromStringAssociationBase
{
    /// <summary>Type discriminator for the entity being referenced.</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Identifier for <see cref="ForEntityType"/> (may be composite, serialized as text).</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Optional actor type that initiated the association or change.</summary>
    public string? FromEntityType { get; set; }

    /// <summary>Optional actor id for <see cref="FromEntityType"/>.</summary>
    public string? FromEntityId { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{GetType().Name}: For={ForEntityType}/{ForEntityId}, From={FromEntityType}/{FromEntityId}";
}
