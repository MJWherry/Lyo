namespace Lyo.EntityReference.Models;

/// <summary>Stable identifiers shared across modules.</summary>
public static class EntityRefWellKnown
{
    /// <summary><see cref="EntityRelationRow.ActorEntityType" /> value used for automated or system attribution when no user actor exists.</summary>
    public const string SystemActorType = "System";

    /// <summary>Fallback tenant used when callers omit an explicit tenant id (single-tenant deployments).</summary>
    public static readonly Guid SingleTenantDefaultId = Guid.Parse("00000000-0000-4000-8000-000000000001");

    /// <summary>Stable id used with <see cref="SystemActorType" /> for persisted actor columns.</summary>
    public static readonly Guid SystemActorId = Guid.Parse("00000000-0000-4000-8000-000000000002");
}