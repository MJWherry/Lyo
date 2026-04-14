using Lyo.People.Models.Enum;

namespace Lyo.People.Models.Relationships;

/// <summary>Represents a relationship between two people</summary>
public class PersonRelationship
{
    /// <summary>Unique identifier for the relationship</summary>
    public Guid Id { get; set; }

    /// <summary>ID of the person this relationship belongs to</summary>
    public Guid PersonId { get; set; }

    /// <summary>ID of the related person</summary>
    public Guid RelatedPersonId { get; set; }

    /// <summary>Type of relationship</summary>
    public RelationshipType Type { get; set; }

    /// <summary>Date when the relationship started</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Date when the relationship ended (null if current)</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Whether the relationship is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether this is a current relationship</summary>
    public bool IsCurrent => EndDate == null;

    /// <summary>Optional notes about the relationship</summary>
    public string? Notes { get; set; }
}