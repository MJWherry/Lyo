namespace Lyo.Billing.Web.Components;

/// <summary>MudBlazor colors for billing enums, matching <c>JobColorHelper</c>.</summary>
public static class BillingColorHelper
{
    /// <summary>Chip color for an invoice lifecycle value.</summary>
    public static Color ForInvoiceStatus(InvoiceStatus status)
        => status switch {
            InvoiceStatus.Draft => Color.Default,
            InvoiceStatus.Issued => Color.Info,
            InvoiceStatus.PartiallyPaid => Color.Warning,
            InvoiceStatus.Paid => Color.Success,
            InvoiceStatus.Void => Color.Error,
            var _ => Color.Default
        };

    /// <summary>Identity hue for a charge kind. Other stays uncolored.</summary>
    public static LyoChipHue? ForChargeKind(ChargeKind kind)
        => kind switch {
            ChargeKind.Time => LyoChipHue.Blue,
            ChargeKind.Expense => LyoChipHue.Amber,
            ChargeKind.Product => LyoChipHue.Lime,
            var _ => null
        };

    /// <summary>Identity hue for a payment method. Other stays uncolored.</summary>
    public static LyoChipHue? ForPaymentMethod(PaymentMethod method)
        => method switch {
            PaymentMethod.Check => LyoChipHue.Blue,
            PaymentMethod.Wire => LyoChipHue.Indigo,
            PaymentMethod.Cash => LyoChipHue.Lime,
            PaymentMethod.Internal => LyoChipHue.Violet,
            var _ => null
        };

    /// <summary>Chip color for a client lifecycle value.</summary>
    public static Color ForClientStatus(BillingClientStatus status)
        => status switch {
            BillingClientStatus.Prospect => Color.Info,
            BillingClientStatus.Active => Color.Success,
            BillingClientStatus.OnHold => Color.Warning,
            BillingClientStatus.Inactive => Color.Default,
            BillingClientStatus.Archived => Color.Dark,
            var _ => Color.Default
        };

    /// <summary>Chip color for a journal lifecycle value.</summary>
    public static Color ForJournalStatus(LedgerJournalStatus status)
        => status switch {
            LedgerJournalStatus.Draft => Color.Default,
            LedgerJournalStatus.Posted => Color.Success,
            LedgerJournalStatus.Voided => Color.Error,
            var _ => Color.Default
        };

    /// <summary>Integer-keyed names for <see cref="Lyo.Web.Components.Models.FilterPropertyDefinition.FromEnum" /> against QueryProject int columns.</summary>
    public static Dictionary<string, string> EnumFilterValues<T>()
        where T : struct, Enum
        => Enum.GetValues<T>().ToDictionary(v => Convert.ToInt32(v).ToString(), v => v.ToString());
}