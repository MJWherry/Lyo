using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Lite person payload owned by billing when People is not wired. Attach via EntityRef type <see cref="BillingEntityTypes.BillingPerson" />.</summary>
[EntityRefLogicalType(BillingEntityTypes.BillingPerson)]
public sealed class BillingPersonRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Given name.</summary>
    public string? GivenName { get; set; }

    /// <summary>Family name.</summary>
    public string? FamilyName { get; set; }

    /// <summary>Job title at the client.</summary>
    public string? Title { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }
}