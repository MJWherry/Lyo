using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity linking a person to an address with type and date range.</summary>
public sealed class ContactAddressEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    public Guid AddressId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "Other"; // Home, Work, Billing, Shipping, Mailing, Other

    public bool IsPrimary { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    [ForeignKey(nameof(PersonId))]
    public PersonEntity? Person { get; set; }

    [ForeignKey(nameof(AddressId))]
    public AddressEntity? Address { get; set; }
}