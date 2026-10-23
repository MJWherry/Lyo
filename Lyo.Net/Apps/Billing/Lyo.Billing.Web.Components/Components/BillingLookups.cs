namespace Lyo.Billing.Web.Components;

/// <summary>Sparse client row for grid FK lookups and the client picker.</summary>
public sealed class ClientLookup
{
    /// <summary>Client id.</summary>
    public Guid Id { get; set; }

    /// <summary>Bill-to display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Legal name.</summary>
    public string? LegalName { get; set; }

    /// <summary>Lifecycle status.</summary>
    public int Status { get; set; }

    /// <summary>True when this client is tax-exempt.</summary>
    public bool TaxExempt { get; set; }

    /// <summary>Default ISO 4217 currency.</summary>
    public string? DefaultCurrency { get; set; }

    /// <summary>Net days used when issuing invoices.</summary>
    public int PaymentTermsDays { get; set; }

    /// <summary>Prefix for issued invoice numbers.</summary>
    public string? InvoicePrefix { get; set; }

    /// <summary>Default notes copied onto new invoices.</summary>
    public string? DefaultInvoiceNotes { get; set; }

    /// <summary>Flattened bill-to contact name.</summary>
    public string? ContactName { get; set; }

    /// <summary>Flattened bill-to line 1.</summary>
    public string? Line1 { get; set; }

    /// <summary>Flattened bill-to line 2.</summary>
    public string? Line2 { get; set; }

    /// <summary>Flattened bill-to city.</summary>
    public string? City { get; set; }

    /// <summary>Flattened bill-to region.</summary>
    public string? Region { get; set; }

    /// <summary>Flattened bill-to postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Flattened bill-to country.</summary>
    public string? Country { get; set; }
}

/// <summary>Create/update payload returned from Clients CRUD.</summary>
public sealed class BillingIdBody
{
    /// <summary>Assigned primary key.</summary>
    public Guid Id { get; set; }
}

/// <summary>Sparse People person for <c>LyoIdColumn</c> route <c>Person</c>.</summary>
public sealed class PersonLookup
{
    /// <summary>Person id.</summary>
    public Guid Id { get; set; }

    /// <summary>Given name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Family name.</summary>
    public string? LastName { get; set; }
}

/// <summary>Sparse People address for route <c>PersonAddress</c>.</summary>
public sealed class PersonAddressLookup
{
    /// <summary>Address id.</summary>
    public Guid Id { get; set; }

    /// <summary>Formatted address.</summary>
    public string? FullAddress { get; set; }

    /// <summary>City.</summary>
    public string? City { get; set; }
}

/// <summary>Sparse People phone for route <c>PersonPhoneNumber</c>.</summary>
public sealed class PersonPhoneLookup
{
    /// <summary>Phone id.</summary>
    public Guid Id { get; set; }

    /// <summary>Number.</summary>
    public string? Number { get; set; }
}

/// <summary>Sparse People email for route <c>PersonEmail</c>.</summary>
public sealed class PersonEmailLookup
{
    /// <summary>Email id.</summary>
    public Guid Id { get; set; }

    /// <summary>Mailbox.</summary>
    public string? Address { get; set; }
}

/// <summary>Sparse billing lite person.</summary>
public sealed class BillingPersonLookup
{
    /// <summary>Payload id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string? DisplayName { get; set; }
}

/// <summary>Sparse billing lite address.</summary>
public sealed class BillingAddressLookup
{
    /// <summary>Payload id.</summary>
    public Guid Id { get; set; }

    /// <summary>Street line 1.</summary>
    public string? Line1 { get; set; }

    /// <summary>City.</summary>
    public string? City { get; set; }
}

/// <summary>Sparse billing lite phone.</summary>
public sealed class BillingPhoneLookup
{
    /// <summary>Payload id.</summary>
    public Guid Id { get; set; }

    /// <summary>Number.</summary>
    public string? Number { get; set; }
}

/// <summary>Sparse billing lite email.</summary>
public sealed class BillingEmailLookup
{
    /// <summary>Payload id.</summary>
    public Guid Id { get; set; }

    /// <summary>Mailbox.</summary>
    public string? Email { get; set; }
}