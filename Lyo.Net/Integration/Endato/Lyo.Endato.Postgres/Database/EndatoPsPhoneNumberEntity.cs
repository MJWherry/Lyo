using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Person Search phone number.</summary>
public sealed class EndatoPsPhoneNumberEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EndatoPersonId { get; set; }

    [ForeignKey(nameof(EndatoPersonId))]
    public EndatoPsPersonEntity EndatoPerson { get; set; } = null!;

    [Required]
    [MaxLength(15)]
    public string Number { get; set; } = null!;

    [MaxLength(100)]
    public string? Company { get; set; }

    [MaxLength(75)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? Type { get; set; }

    [Required]
    public bool IsConnected { get; set; }

    [Required]
    public bool IsPublic { get; set; }

    public NpgsqlPoint? Coordinates { get; set; }

    [Required]
    public int OrderNumber { get; set; }

    [Required]
    public DateOnly FirstReportedDate { get; set; }

    [Required]
    public DateOnly LastReportedDate { get; set; }

    [Required]
    public DateOnly PublicFirstSeenDate { get; set; }

    [Required]
    public DateOnly TotalFirstSeenDate { get; set; }
}