using System.Diagnostics;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>
/// EF columns for a string-keyed subject entity plus optional actor (for example audit / change history).
/// Not the tenant-scoped <see cref="EntityRelationEntityBase" /> row with lifecycle columns.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class EntityRelationOptionalActorBase : EntityRelationEndpointsEntityBase
{
    /// <summary>Optional tenant scope. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name}: Subject={SubjectEntityType}/{SubjectEntityId}, Actor={ActorEntityType}/{ActorEntityId}, Tenant={TenantId}";
}
