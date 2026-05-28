using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.EntityReference.Postgres;

/// <summary>Maps between <see cref="EntitySourceRecord" /> and <see cref="EntitySourceEntityBase" />.</summary>
public static class EntitySourceMapping
{
    public static EntitySourceRecord ToRecord(EntitySourceEntityBase entity)
        => new(
            EntityRef.ForKey(entity.SourceEntityType, entity.SourceEntityId),
            entity.ImportedAt,
            entity.FromEntityType is { Length: > 0 } ft && entity.FromEntityId is { Length: > 0 } fi
                ? EntityRef.ForKey(ft, fi)
                : null);

    public static TEntity ToEntity<TEntity>(EntitySourceRecord record, Guid parentId, Func<Guid, TEntity> factory)
        where TEntity : EntitySourceEntityBase
    {
        var entity = factory(parentId);
        entity.Id = Guid.NewGuid();
        entity.SourceEntityType = record.Source.EntityType;
        entity.SourceEntityId = record.Source.EntityId;
        entity.ImportedAt = record.ImportedAt;
        if (record.ImportedFrom is { } from) {
            entity.FromEntityType = from.EntityType;
            entity.FromEntityId = from.EntityId;
        }

        return entity;
    }

    public static void SyncSources<TEntity>(
        ICollection<TEntity> entities,
        IEnumerable<EntitySourceRecord> records,
        Guid parentId,
        Func<Guid, TEntity> factory)
        where TEntity : EntitySourceEntityBase
    {
        entities.Clear();
        foreach (var record in records)
            entities.Add(ToEntity(record, parentId, factory));
    }
}
