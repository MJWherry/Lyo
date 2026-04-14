using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Contact Enrichment phone number.</summary>
public sealed class EndatoCePhoneNumberEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EndatoCePersonId { get; set; }

    [ForeignKey(nameof(EndatoCePersonId))]
    public EndatoCePersonEntity EndatoCePerson { get; set; } = null!;

    [Required]
    [MaxLength(18)]
    public string Number { get; set; } = null!;

    [Required]
    [MaxLength(15)]
    public string Type { get; set; } = null!;

    [Required]
    public bool IsConnected { get; set; }

    [Required]
    public DateOnly FirstReportedDate { get; set; }

    [Required]
    public DateOnly LastReportedDate { get; set; }
}