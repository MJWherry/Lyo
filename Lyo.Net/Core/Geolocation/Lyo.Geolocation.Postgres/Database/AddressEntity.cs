using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lyo.EntityReference.Postgres.Database;
using NpgsqlTypes;

namespace Lyo.Geolocation.Postgres.Database;

/// <summary>Canonical address row in the geolocation schema.</summary>
public sealed class AddressEntity : EntitySourceDerivedEntityBase
{
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

    [MaxLength(200)]
    public string? StreetAddress { get; set; }

    [MaxLength(200)]
    public string? StreetAddressLine2 { get; set; }

    [MaxLength(8)]
    public string? Unit { get; set; }

    [MaxLength(12)]
    public string? UnitType { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? SubLocality { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string? Province { get; set; }

    [MaxLength(5)]
    public string? Zipcode { get; set; }

    [MaxLength(4)]
    public string? Zipcode4 { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [Required]
    [MaxLength(3)]
    public string CountryCode { get; set; } = "US";

    [MaxLength(50)]
    public string? County { get; set; }

    [MaxLength(50)]
    public string? SubAdministrativeArea { get; set; }

    [MaxLength(200)]
    public string? FullAddress { get; set; }

    public NpgsqlPoint? Coordinates { get; set; }

    public bool? IsDeliverable { get; set; }

    public bool? IsMergedAddress { get; set; }

    public bool? IsPublic { get; set; }

    [MaxLength(32)]
    public string? PropertyIndicator { get; set; }

    [MaxLength(32)]
    public string? BldgCode { get; set; }

    [MaxLength(32)]
    public string? UtilityCode { get; set; }

    public int? UnitCount { get; set; }

    public DateOnly? FirstReportedDate { get; set; }

    public DateOnly? LastReportedDate { get; set; }

    public DateOnly? PublicFirstSeenDate { get; set; }

    public double? GeocodeConfidence { get; set; }

    [Column(TypeName = "jsonb")]
    public string? MetadataJson { get; set; }
}