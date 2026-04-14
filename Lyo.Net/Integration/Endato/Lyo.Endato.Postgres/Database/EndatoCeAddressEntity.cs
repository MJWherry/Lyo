using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Contact Enrichment address.</summary>
public sealed class EndatoCeAddressEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EndatoCePersonId { get; set; }

    [ForeignKey(nameof(EndatoCePersonId))]
    public EndatoCePersonEntity EndatoCePerson { get; set; } = null!;

    [Required]
    [MaxLength(75)]
    public string Street { get; set; } = null!;

    [MaxLength(8)]
    public string? Unit { get; set; }

    [MaxLength(25)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }

    [MaxLength(12)]
    public string? Zipcode { get; set; }

    [Required]
    public DateOnly FirstReportedDate { get; set; }

    [Required]
    public DateOnly LastReportedDate { get; set; }
}