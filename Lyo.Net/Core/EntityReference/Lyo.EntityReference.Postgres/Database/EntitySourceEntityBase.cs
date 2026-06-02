using System.Diagnostics;

namespace Lyo.EntityReference.Postgres.Database;

/// <summary>Shared source provenance columns for parent rows (maps to <c>source_entity_*</c> / <c>imported_at</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class EntitySourceEntityBase
{
    /// <summary>External source type discriminator.</summary>
    public string? SourceEntityType { get; set; }

    /// <summary>External source identifier for <see cref="SourceEntityType" />.</summary>
    public string? SourceEntityId { get; set; }

    /// <summary>When this row was imported from <see cref="SourceEntityType" /> / <see cref="SourceEntityId" />.</summary>
    public DateTime? ImportedAt { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => string.IsNullOrEmpty(SourceEntityType) ? $"{GetType().Name}: unsourced" : $"{GetType().Name}: {SourceEntityType}/{SourceEntityId}, ImportedAt={ImportedAt}";
}