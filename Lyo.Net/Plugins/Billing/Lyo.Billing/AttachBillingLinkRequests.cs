using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>Body for <c>POST /Billing/Clients/{clientId}/contacts</c>.</summary>
public sealed class AttachBillingContactRequest
{
    /// <summary>Optional tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Party to attach (People <c>Person</c> or a billing lite person).</summary>
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

/// <summary>Body for <c>POST /Billing/Clients/{clientId}/phones</c>.</summary>
public sealed class AttachBillingPhoneRequest
{
    /// <summary>Optional tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Phone payload EntityRef.</summary>
    public EntityRef Phone { get; set; }

    /// <summary>How this number is used.</summary>
    public BillingPhoneUsage Usage { get; set; }

    /// <summary>True when this is the primary number.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Optional contact this number belongs to.</summary>
    public EntityRef? Contact { get; set; }
}

/// <summary>Body for <c>POST /Billing/Clients/{clientId}/emails</c>.</summary>
public sealed class AttachBillingEmailRequest
{
    /// <summary>Optional tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Email payload EntityRef.</summary>
    public EntityRef Email { get; set; }

    /// <summary>How this mailbox is used.</summary>
    public BillingEmailUsage Usage { get; set; }

    /// <summary>True when this is the primary mailbox.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Optional contact this mailbox belongs to.</summary>
    public EntityRef? Contact { get; set; }
}

/// <summary>Body for <c>POST /Billing/Clients/{clientId}/addresses</c>.</summary>
public sealed class AttachBillingAddressRequest
{
    /// <summary>Optional tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Address payload EntityRef.</summary>
    public EntityRef Address { get; set; }

    /// <summary>How this address is used.</summary>
    public BillingAddressUsage Usage { get; set; }

    /// <summary>True when this is the primary address of this usage.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Optional contact this address belongs to.</summary>
    public EntityRef? Contact { get; set; }
}
