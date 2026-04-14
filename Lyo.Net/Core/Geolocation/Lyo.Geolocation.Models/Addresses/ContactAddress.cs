using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models.Addresses;

/// <summary>Links a person to an address with relationship type (home, work, etc.) This allows a person to have multiple addresses of different types</summary>
public class ContactAddress
{
    public Guid Id { get; set; }

    public Guid PersonId { get; set; }

    public Guid AddressId { get; set; }

    public Address? Address { get; set; } // Navigation property to the actual address

    public ContactAddressType Type { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime? StartDate { get; set; } // When person started using this address

    public DateTime? EndDate { get; set; } // When person stopped using this address (null = current)

    public bool IsCurrent => EndDate == null;

    public string? Notes { get; set; } // Additional notes about this address relationship
}