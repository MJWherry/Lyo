using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Contact Enrichment email address.</summary>
public sealed class EndatoCeEmailAddressEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EndatoCePersonId { get; set; }

    [ForeignKey(nameof(EndatoCePersonId))]
    public EndatoCePersonEntity EndatoCePerson { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = null!;

    [Required]
    public bool IsValidated { get; set; }

    [Required]
    public bool IsBusiness { get; set; }
}