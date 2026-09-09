using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>EF entity base for the canonical relation row (PostgreSQL).</summary>
/// <remarks>Change-tracker and similar modules that keep arbitrary string keys and optional actors should use <see cref="EntityRelationOptionalActorBase" /> instead.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class EntityRelationEntityBase : EntityRelationEndpointsEntityBase
{
    /// <summary>Row primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant this row belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Optional context label.</summary>
    public string? Context { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Optional expiry instant (UTC).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>When the row was soft-deleted (UTC).</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Actor type that soft-deleted the row.</summary>
    public string? DeletedByType { get; set; }

    /// <summary>Actor id that soft-deleted the row.</summary>
    public Guid? DeletedById { get; set; }

    /// <summary>Metadata payload (<c>jsonb</c>).</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Visibility label; defaults to <see cref="EntityRefVisibility.Private" />.</summary>
    public string Visibility { get; set; } = EntityRefVisibility.Private;

    /// <inheritdoc />
    public override string ToString()
        => $"{GetType().Name}: Id={Id}, Tenant={TenantId}, Subject={SubjectEntityType}/{SubjectEntityId}, Actor={ActorEntityType}/{ActorEntityId}, Visibility={Visibility}, DeletedAt={DeletedAt}";
}