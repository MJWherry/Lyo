using System.Globalization;

namespace Lyo.Billing.Web.Components;

/// <summary>Formats invoice and payment amounts with an ISO currency code.</summary>
public static class BillingMoney
{
    /// <summary>Renders <paramref name="amount"/> with two fraction digits and a currency code.</summary>
    public static string Format(decimal amount, string? currency)
        => string.Create(CultureInfo.InvariantCulture, $"{amount:N2} {(string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim())}");

    /// <summary>Renders a projected numeric field, or an em dash when missing.</summary>
    public static string Format(object? amount, string? currency)
    {
        if (amount is null)
            return "—";
        if (amount is decimal d)
            return Format(d, currency);
        if (decimal.TryParse(Convert.ToString(amount, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return Format(parsed, currency);
        return Convert.ToString(amount, CultureInfo.InvariantCulture) ?? "—";
    }
}
