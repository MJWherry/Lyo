using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.EntityReference.Postgres;

/// <summary>Maps subject/actor endpoints between domain and EF relation rows.</summary>
public static class EntityRelationMapping
{
    public static void ApplySubjectActor(EntityRelationEndpointsEntityBase entity, EntityRef subject, EntityRef actor)
    {
        entity.SubjectEntityType = subject.EntityType;
        entity.SubjectEntityId = subject.EntityId;
        entity.ActorEntityType = actor.EntityType;
        entity.ActorEntityId = actor.EntityId;
    }

    public static void ApplySubjectActor(EntityRelationEndpointsEntityBase entity, EntityRelationRow row)
    {
        entity.SubjectEntityType = row.SubjectEntityType;
        entity.SubjectEntityId = row.SubjectEntityId;
        entity.ActorEntityType = row.ActorEntityType;
        entity.ActorEntityId = row.ActorEntityId;
    }

    public static void ToRelationRow(EntityRelationEndpointsEntityBase entity, EntityRelationRow row)
    {
        row.SubjectEntityType = entity.SubjectEntityType;
        row.SubjectEntityId = entity.SubjectEntityId;
        row.ActorEntityType = entity.ActorEntityType;
        row.ActorEntityId = entity.ActorEntityId;
    }
}