using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity for storing identification documents in PostgreSQL.</summary>
public sealed class IdentificationEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    [MaxLength(30)]
    public string Type { get; set; } = "Other"; // Passport, DriversLicense, NationalId, SSN, TaxId, VoterId, Other

    [Required]
    [MaxLength(100)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(3)]
    public string? IssuingCountry { get; set; }

    [MaxLength(200)]
    public string? IssuingAuthority { get; set; }

    public DateOnly? IssueDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public bool IsVerified { get; set; }

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    [ForeignKey(nameof(PersonId))]
    public PersonEntity? Person { get; set; }
}