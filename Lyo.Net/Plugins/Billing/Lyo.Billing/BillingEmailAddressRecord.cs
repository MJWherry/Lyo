using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Lite email payload owned by billing. Attach via EntityRef type <see cref="BillingEntityTypes.BillingEmailAddress"/>.</summary>
[EntityRefLogicalType(BillingEntityTypes.BillingEmailAddress)]
public sealed class BillingEmailAddressRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Mailbox.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }
}
