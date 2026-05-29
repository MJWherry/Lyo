namespace Lyo.EntityReference.Postgres.Database;

/// <summary>EF base for <c>*_source</c> provenance child tables.</summary>
public abstract class EntitySourceEntityBase
{
    public Guid Id { get; set; }

    public string SourceEntityType { get; set; } = string.Empty;

    public string SourceEntityId { get; set; } = string.Empty;

    public DateTime ImportedAt { get; set; }

    public string? FromEntityType { get; set; }

    public string? FromEntityId { get; set; }
}