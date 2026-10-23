namespace Lyo.Billing;

/// <summary>Display helpers for tax identifiers. Full values are encrypted at rest.</summary>
public static class BillingTaxId
{
    /// <summary>Returns the last four characters of <paramref name="taxId" /> after trimming, or null when empty.</summary>
    public static string? Last4(string? taxId)
    {
        if (taxId is null)
            return null;

        var trimmed = taxId.Trim();
        if (trimmed.Length == 0)
            return null;

        return trimmed.Length < 4 ? trimmed : trimmed.Substring(trimmed.Length - 4);
    }
}