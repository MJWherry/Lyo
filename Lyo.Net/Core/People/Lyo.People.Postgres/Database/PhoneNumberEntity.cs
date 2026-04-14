using System.ComponentModel.DataAnnotations;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity for storing phone numbers in PostgreSQL.</summary>
public sealed class PhoneNumberEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(3)]
    public string? CountryCode { get; set; }

    [MaxLength(10)]
    public string? CountryCodeString { get; set; }

    [MaxLength(20)]
    public string? TechnologyType { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [MaxLength(100)]
    public string? Label { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}