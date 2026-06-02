namespace Lyo.EntityReference.Postgres.Database;

/// <summary>EF base for aggregates that carry provenance and may diverge from external source(s).</summary>
public abstract class EntitySourceDerivedEntityBase : EntitySourceEntityBase
{
    public Guid Id { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Set when content was edited after import and may no longer match source(s).</summary>
    public DateTime? LocallyModifiedAt { get; set; }
}