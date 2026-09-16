namespace Lyo.Ledger;

/// <summary>Chart-of-accounts row. Codes are unique per tenant.</summary>
public sealed class LedgerAccountRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Human account number (for example <c>1100</c>).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Normal-balance classification.</summary>
    public LedgerAccountType AccountType { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
