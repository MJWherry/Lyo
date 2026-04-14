using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Entity representing an Endato Person Search address.</summary>
public sealed class EndatoPsAddressEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EndatoPersonId { get; set; }

    [ForeignKey(nameof(EndatoPersonId))]
    public EndatoPsPersonEntity EndatoPerson { get; set; } = null!;

    [Required]
    public bool IsDeliverable { get; set; }

    [Required]
    public bool IsMergedAddress { get; set; }

    [Required]
    public bool IsPublic { get; set; }

    [Required]
    [MaxLength(25)]
    public string AddressHash { get; set; } = null!;

    [MaxLength(12)]
    public string? HouseNumber { get; set; }

    [MaxLength(12)]
    public string? StreetPreDirection { get; set; }

    [MaxLength(50)]
    public string? StreetName { get; set; }

    [MaxLength(12)]
    public string? StreetPostDirection { get; set; }

    [MaxLength(12)]
    public string? StreetType { get; set; }

    [MaxLength(8)]
    public string? Unit { get; set; }

    [MaxLength(12)]
    public string? UnitType { get; set; }

    [MaxLength(25)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string? County { get; set; }

    [MaxLength(5)]
    public string? Zipcode { get; set; }

    [MaxLength(4)]
    public string? Zipcode4 { get; set; }

    [MaxLength(100)]
    public string? FullAddress { get; set; }

    public NpgsqlPoint? Coordinates { get; set; }

    public string[]? PhoneNumbers { get; set; }

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