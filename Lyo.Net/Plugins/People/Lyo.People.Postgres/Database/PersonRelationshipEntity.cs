using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Lyo.People.Postgres.Database;

/// <summary>PostgreSQL row for a person-to-person relationship.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PersonRelationshipEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    public Guid RelatedPersonId { get; set; }

    [Required]
    [MaxLength(30)]
    public string Type { get; set; } = "Other";

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    [ForeignKey(nameof(PersonId))]
    public PersonEntity? Person { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}