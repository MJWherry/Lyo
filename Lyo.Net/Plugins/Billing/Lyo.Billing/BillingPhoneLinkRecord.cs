using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Link from a billing client to a phone payload.</summary>
public sealed class BillingPhoneLinkRecord
{
    /// <summary>Link row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning billing client.</summary>
    public Guid ClientId { get; set; }

    /// <summary>Phone payload EntityRef.</summary>
    public EntityRef Phone { get; set; }

    /// <summary>Usage of this number on the client.</summary>
    public BillingPhoneUsage Usage { get; set; }

    /// <summary>True when this is the primary number.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Optional contact this number belongs to.</summary>
    public EntityRef? Contact { get; set; }
}