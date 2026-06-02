using System.Diagnostics;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models.Addresses;

/// <summary>Links a person to an address with relationship type (home, work, etc.) This allows a person to have multiple addresses of different types</summary>
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

    public override string ToString()
        => $"ContactAddress: person={PersonId}, address={AddressId}, type={Type}, primary={IsPrimary}";
}
