namespace Lyo.Billing;

/// <summary>Computes stored invoice-line amounts for <see cref="InvoiceLinePricing"/>.</summary>
public static class InvoiceLinePricingMath
{
    /// <summary>Validates quantity and unit amount for <paramref name="pricing"/>.</summary>
    public static void Validate(InvoiceLinePricing pricing, decimal quantity, decimal unitAmount)
    {
        switch (pricing) {
            case InvoiceLinePricing.Discount:
                if (unitAmount <= 0)
                    throw new ArgumentException("Discount must be a positive amount.", nameof(unitAmount));
                break;
            case InvoiceLinePricing.PercentDiscount:
                if (unitAmount < 0 || unitAmount > 100)
                    throw new ArgumentException("Percent discount must be between 0 and 100.", nameof(unitAmount));
                break;
            default:
                if (quantity <= 0)
                    throw new ArgumentException("Line quantity must be positive.", nameof(quantity));
                if (unitAmount < 0)
                    throw new ArgumentException("Unit amount cannot be negative.", nameof(unitAmount));
                break;
        }
    }

    /// <summary>
    /// Amount stored on the line. <paramref name="runningSubtotal"/> is the sum of earlier lines (used only for <see cref="InvoiceLinePricing.PercentDiscount"/>).
    /// </summary>
    public static decimal Compute(InvoiceLinePricing pricing, decimal quantity, decimal unitAmount, decimal runningSubtotal)
    {
        switch (pricing) {
            case InvoiceLinePricing.Discount:
                return decimal.Round(-Math.Abs(unitAmount), 4, MidpointRounding.AwayFromZero);
            case InvoiceLinePricing.PercentDiscount:
                if (runningSubtotal <= 0 || unitAmount <= 0)
                    return 0m;
                return decimal.Round(-(runningSubtotal * unitAmount / 100m), 4, MidpointRounding.AwayFromZero);
            default:
                return decimal.Round(quantity * unitAmount, 4, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>Quantity persisted for fixed-discount and percent-discount lines.</summary>
    public static decimal NormalizedQuantity(InvoiceLinePricing pricing, decimal quantity)
        => pricing is InvoiceLinePricing.Discount or InvoiceLinePricing.PercentDiscount ? 1m : quantity;
}
