using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Person Search email address.</summary>
public sealed class EndatoPsEmailAddressEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EndatoPersonId { get; set; }

    [ForeignKey(nameof(EndatoPersonId))]
    public EndatoPsPersonEntity EndatoPerson { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Address { get; set; } = null!;

    [Required]
    public int OrderNumber { get; set; }

    [Required]
    public bool IsPremium { get; set; }

    [Required]
    public bool NonBusiness { get; set; }
}