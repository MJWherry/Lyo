namespace Lyo.EntityReference.Postgres.Database;

/// <summary>EF base for aggregates that carry provenance and may diverge from an external source.</summary>
public abstract class EntitySourceDerivedEntityBase : EntitySourceEntityBase
{
    public Guid Id { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Set when content was changed after import and may no longer match the source.</summary>
    public DateTime? LocallyModifiedAt { get; set; }
}