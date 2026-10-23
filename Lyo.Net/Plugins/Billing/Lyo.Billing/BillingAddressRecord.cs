using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Lite address payload owned by billing. Attach via EntityRef type <see cref="BillingEntityTypes.BillingAddress" />.</summary>
[EntityRefLogicalType(BillingEntityTypes.BillingAddress)]
public sealed class BillingAddressRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Street line 1.</summary>
    public string? Line1 { get; set; }

    /// <summary>Street line 2.</summary>
    public string? Line2 { get; set; }

    /// <summary>City.</summary>
    public string? City { get; set; }

    /// <summary>State / region / province.</summary>
    public string? Region { get; set; }

    /// <summary>Postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>ISO 3166 country.</summary>
    public string? Country { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }
}