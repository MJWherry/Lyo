using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Link from a billing client to an email payload.</summary>
public sealed class BillingEmailLinkRecord
{
    /// <summary>Link row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning billing client.</summary>
    public Guid ClientId { get; set; }

    /// <summary>Email payload EntityRef.</summary>
    public EntityRef Email { get; set; }

    /// <summary>Usage of this mailbox on the client.</summary>
    public BillingEmailUsage Usage { get; set; }

    /// <summary>True when this is the primary mailbox.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Optional contact this mailbox belongs to.</summary>
    public EntityRef? Contact { get; set; }
}