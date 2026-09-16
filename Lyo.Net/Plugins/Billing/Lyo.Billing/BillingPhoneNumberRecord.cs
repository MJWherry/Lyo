using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Lite phone payload owned by billing. Attach via EntityRef type <see cref="BillingEntityTypes.BillingPhoneNumber"/>.</summary>
[EntityRefLogicalType(BillingEntityTypes.BillingPhoneNumber)]
public sealed class BillingPhoneNumberRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Number in E.164 form when known.</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>ISO 3166 country of the number.</summary>
    public string? Country { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }
}
