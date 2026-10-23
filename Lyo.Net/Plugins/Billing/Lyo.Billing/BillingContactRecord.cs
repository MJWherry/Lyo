using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Link from a billing client to a party (People <c>Person</c> or <see cref="BillingPersonRecord" />).</summary>
public sealed class BillingContactRecord
{
    /// <summary>Link row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning billing client.</summary>
    public Guid ClientId { get; set; }

    /// <summary>The party. Type is <see cref="BillingEntityTypes.Person" /> or <see cref="BillingEntityTypes.BillingPerson" />.</summary>
    public EntityRef Contact { get; set; }

    /// <summary>Role at this client.</summary>
    public BillingContactRole Role { get; set; }

    /// <summary>Title override on this link.</summary>
    public string? Title { get; set; }

    /// <summary>True when this is the client's primary contact.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>True when this contact receives invoices.</summary>
    public bool IsBillingContact { get; set; }
}