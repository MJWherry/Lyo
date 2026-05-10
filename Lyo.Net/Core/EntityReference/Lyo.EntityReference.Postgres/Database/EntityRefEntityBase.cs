using System.Diagnostics;
using Lyo.EntityReference.Models;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>EF entity base mapping the canonical association row (PostgreSQL).</summary>
/// <remarks>
/// Change-tracker and similar modules that keep arbitrary string keys (including composite ids) and optional actors
/// should use <see cref="EntityRefOptionalFromStringAssociationBase"/> instead — they are not Option A uuid tenant rows.
/// </remarks>
[DebuggerDisplay("{ForEntityType,nq}:{ForEntityId} | {FromEntityType,nq}:{FromEntityId} | Tenant={TenantId}")]
public abstract class EntityRefEntityBase
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Type discriminator for the entity being referenced.</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Entity id for <see cref="ForEntityType"/> (PostgreSQL <c>uuid</c>).</summary>
    public Guid ForEntityId { get; set; }

    /// <summary>Type discriminator for the originating actor entity.</summary>
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Entity id for <see cref="FromEntityType"/> (PostgreSQL <c>uuid</c>).</summary>
    public Guid FromEntityId { get; set; }

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

    /// <summary>Visibility label (defaults to <see cref="EntityRefVisibility.Private"/>).</summary>
    public string Visibility { get; set; } = EntityRefVisibility.Private;

    /// <inheritdoc />
    public override string ToString() =>
        $"{GetType().Name}: Id={Id}, Tenant={TenantId}, For={ForEntityType}/{ForEntityId}, From={FromEntityType}/{FromEntityId}, Visibility={Visibility}, DeletedAt={DeletedAt}";
}
