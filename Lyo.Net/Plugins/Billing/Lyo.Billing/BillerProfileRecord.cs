namespace Lyo.Billing;

/// <summary>Your firm on invoices. Bank/remittance secrets are plaintext on this record after decrypt.</summary>
public sealed class BillerProfileRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope. One profile per tenant is typical.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Legal name.</summary>
    public string? LegalName { get; set; }

    /// <summary>Remittance instructions (plaintext in memory).</summary>
    public string? Remittance { get; set; }

    /// <summary>Bank account number (plaintext in memory).</summary>
    public string? BankAccountNumber { get; set; }

    /// <summary>Last four of the bank account for display.</summary>
    public string? BankAccountLast4 { get; set; }
}
