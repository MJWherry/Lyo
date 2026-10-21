namespace Lyo.Billing;

/// <summary>How an invoice line's <see cref="InvoiceLineRecord.Amount" /> is derived from quantity and unit amount.</summary>
public enum InvoiceLinePricing
{
    /// <summary>Quantity × unit amount (the default).</summary>
    Amount = 0,

    /// <summary>Fixed money off. Unit amount is the discount; stored amount is negative.</summary>
    Discount = 1,

    /// <summary>Percent discount off the running subtotal of earlier lines (0–100). Stored amount is negative.</summary>
    PercentDiscount = 2
}