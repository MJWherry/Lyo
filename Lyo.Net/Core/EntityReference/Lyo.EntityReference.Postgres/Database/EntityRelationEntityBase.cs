using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>EF entity base mapping the canonical relation row (PostgreSQL).</summary>
/// <remarks>Change-tracker and similar modules that keep arbitrary string keys and optional actors should use <see cref="EntityRelationOptionalActorBase" /> instead.</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class EntityRelationEntityBase : EntityRelationEndpointsEntityBase
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Optional scope label.</summary>
    public string? Context { get; set; }

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Optional expiry (UTC).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Soft-delete time (UTC).</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Actor type that soft-deleted this row.</summary>
    public string? DeletedByType { get; set; }

    /// <summary>Actor id that soft-deleted this row.</summary>
    public Guid? DeletedById { get; set; }

    /// <summary>JSON metadata (<c>jsonb</c>).</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Visibility label (defaults to <see cref="EntityRefVisibility.Private" />).</summary>
    public string Visibility { get; set; } = EntityRefVisibility.Private;

    /// <inheritdoc />
    public override string ToString()
        => $"{GetType().Name}: Id={Id}, Tenant={TenantId}, Subject={SubjectEntityType}/{SubjectEntityId}, Actor={ActorEntityType}/{ActorEntityId}, Visibility={Visibility}, DeletedAt={DeletedAt}";
}