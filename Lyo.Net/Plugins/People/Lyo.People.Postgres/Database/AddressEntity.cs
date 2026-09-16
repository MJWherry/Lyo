using System.ComponentModel.DataAnnotations;
using Lyo.EntityReference.Postgres.Database;
using NpgsqlTypes;

namespace Lyo.People.Postgres.Database;

/// <summary>PostgreSQL address row. Follows the Endato street shape (pre/post direction, street type, and so on).</summary>
public sealed class AddressEntity : EntitySourceEntityBase
{
    [Key]
    public Guid Id { get; set; }

    // US street parts (Endato layout)
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

    // Full street lines (international-style)
    [MaxLength(200)]
    public string? StreetAddress { get; set; }

    [MaxLength(200)]
    public string? StreetAddressLine2 { get; set; }

    // Locality columns
    [MaxLength(25)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string? County { get; set; }

    // Postal columns
    [MaxLength(5)]
    public string? Zipcode { get; set; }

    [MaxLength(4)]
    public string? Zipcode4 { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [Required]
    [MaxLength(3)]
    public string CountryCode { get; set; } = "US";

    // Formatted / computed
    [MaxLength(200)]
    public string? FullAddress { get; set; }

    public NpgsqlPoint? Coordinates { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}