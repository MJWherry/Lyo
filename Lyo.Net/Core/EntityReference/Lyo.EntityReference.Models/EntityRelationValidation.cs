namespace Lyo.EntityReference.Models;

/// <summary>Validates subject/actor endpoint pairs on relation rows.</summary>
public static class EntityRelationValidation
{
    /// <summary>Requires both relation endpoints (favorite, note, comment, …).</summary>
    public static void RequireSubjectActor(EntityRef? subject, EntityRef? actor)
    {
        if (subject is null || actor is null)
            throw new ArgumentException("Relation requires both subject and actor EntityRef endpoints.");
    }

    /// <summary>Requires both relation endpoints from an <see cref="EntityRelationEndpoints" /> value.</summary>
    public static void RequireSubjectActor(EntityRelationEndpoints endpoints)
        => RequireSubjectActor(endpoints.Subject, endpoints.Actor);
}
