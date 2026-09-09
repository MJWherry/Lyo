using System.Diagnostics;
using Lyo.People.Models.Enum;

namespace Lyo.People.Models.Relationships;

/// <summary>Link between two people.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonRelationship
{
    /// <summary>Primary key for this relationship row.</summary>
    public Guid Id { get; set; }

    /// <summary>Person who owns this relationship.</summary>
    public Guid PersonId { get; set; }

    /// <summary>The other person in the relationship.</summary>
    public Guid RelatedPersonId { get; set; }

    /// <summary>Kind of relationship.</summary>
    public RelationshipType Type { get; set; }

    /// <summary>When the relationship began.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>When the relationship ended; null while current.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>True when the relationship is treated as active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>True when there is no end date.</summary>
    public bool IsCurrent => EndDate == null;

    /// <summary>Free-text notes about the relationship.</summary>
    public string? Notes { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"PersonRelationship: person={PersonId}, related={RelatedPersonId}, type={Type}, active={IsActive}";
}
