using System.Diagnostics;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models.Addresses;

/// <summary>Links a person to an address with a relationship type (home, work, and similar) so one person can hold several addresses.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ContactAddress
{
    public Guid Id { get; set; }

    public Guid PersonId { get; set; }

    public Guid AddressId { get; set; }

    public Address? Address { get; set; }

    public ContactAddressType Type { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsCurrent => EndDate == null;

    public string? Notes { get; set; }

    public override string ToString() => $"ContactAddress: person={PersonId}, address={AddressId}, type={Type}, primary={IsPrimary}";
}