using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

namespace Lyo.People.Postgres.Database;

/// <summary>Entity for storing addresses in PostgreSQL. Based on Endato address structure with street pre/post direction, street type, etc.</summary>
public sealed class AddressEntity
{
    [Key]
    public Guid Id { get; set; }

    // US-style street components (Endato format)
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

    // Alternative: full street address (international-style)
    [MaxLength(200)]
    public string? StreetAddress { get; set; }

    [MaxLength(200)]
    public string? StreetAddressLine2 { get; set; }

    // Locality
    [MaxLength(25)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string? County { get; set; }

    // Postal
    [MaxLength(5)]
    public string? Zipcode { get; set; }

    [MaxLength(4)]
    public string? Zipcode4 { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [Required]
    [MaxLength(3)]
    public string CountryCode { get; set; } = "US";

    // Computed/formatted
    [MaxLength(200)]
    public string? FullAddress { get; set; }

    public NpgsqlPoint? Coordinates { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}