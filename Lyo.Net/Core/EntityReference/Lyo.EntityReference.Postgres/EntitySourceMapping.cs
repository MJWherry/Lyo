using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.EntityReference.Postgres;

/// <summary>Maps between <see cref="EntitySourceRecord" /> and <see cref="EntitySourceEntityBase" />.</summary>
public static class EntitySourceMapping
{
    public static EntitySourceRecord? ToRecord(EntitySourceEntityBase entity)
    {
        if (string.IsNullOrEmpty(entity.SourceEntityType) || string.IsNullOrEmpty(entity.SourceEntityId) || entity.ImportedAt is null)
            return null;

        return EntitySourceRecord.From(
            EntityRef.ForKey(entity.SourceEntityType, entity.SourceEntityId),
            entity.ImportedAt.Value);
    }

    public static void ApplySource(EntitySourceEntityBase entity, EntitySourceRecord? record)
    {
        if (record is not { } value) {
            entity.SourceEntityType = null;
            entity.SourceEntityId = null;
            entity.ImportedAt = null;
            return;
        }

        entity.SourceEntityType = value.Source.EntityType;
        entity.SourceEntityId = value.Source.EntityId;
        entity.ImportedAt = value.ImportedAt;
    }

    /// <summary>Marks a source-derived aggregate as locally modified when it has a source and content changed.</summary>
    public static void MarkLocallyModifiedIfSourced(IEntitySourceDerived entity, bool contentChanged)
    {
        if (contentChanged && entity.HasSource())
            entity.LocallyModifiedAt = DateTime.UtcNow;
    }
}
