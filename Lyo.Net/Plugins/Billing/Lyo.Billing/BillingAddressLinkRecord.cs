using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Link from a billing client to an address payload.</summary>
public sealed class BillingAddressLinkRecord
{
    /// <summary>Link row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning billing client.</summary>
    public Guid ClientId { get; set; }

    /// <summary>Address payload EntityRef.</summary>
    public EntityRef Address { get; set; }

    /// <summary>Usage of this address on the client.</summary>
    public BillingAddressUsage Usage { get; set; }

    /// <summary>True when this is the primary address.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Optional contact this address belongs to.</summary>
    public EntityRef? Contact { get; set; }
}
