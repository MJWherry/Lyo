using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Row that stores an Endato Contact Enrichment phone number.</summary>
[DebuggerDisplay("{ToString(),nq}")]
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

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}