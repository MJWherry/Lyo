namespace Lyo.Billing;

/// <summary>Frozen invoice line (copied from a charge or entered ad hoc).</summary>
public sealed class InvoiceLineRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Parent invoice.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>Source charge when billed from unbilled work.</summary>
    public Guid? ChargeId { get; set; }

    /// <summary>1-based order.</summary>
    public int LineNumber { get; set; }

    /// <summary>Short line title frozen at issue.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description under <see cref="Title"/>.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Quantity. Discount lines persist 1.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Unit amount, fixed discount money, or percent discount (0–100) depending on <see cref="Pricing"/>.</summary>
    public decimal UnitAmount { get; set; }

    /// <summary>Stored line amount (negative for fixed and percent discounts).</summary>
    public decimal Amount { get; set; }

    /// <summary>How <see cref="Amount"/> is derived. Default is quantity × unit amount.</summary>
    public InvoiceLinePricing Pricing { get; set; }
}
