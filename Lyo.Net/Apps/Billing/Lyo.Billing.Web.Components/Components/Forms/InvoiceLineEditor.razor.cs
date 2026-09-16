using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Forms;

public partial class InvoiceLineEditor
{
    [Parameter]
    public IReadOnlyList<InvoiceLineRecord> Lines { get; set; } = [];

    [Parameter]
    public bool AllowEdit { get; set; }

    [Parameter]
    public EventCallback<InvoiceLineRecord> Add { get; set; }

    [Parameter]
    public EventCallback<InvoiceLineRecord> Remove { get; set; }

    private IReadOnlyList<InvoiceLineRecord> _lines => Lines;

    private readonly InvoiceLineRecord _draft = new() { Quantity = 1m, Pricing = InvoiceLinePricing.Amount };

    private string UnitLabel => _draft.Pricing switch {
        InvoiceLinePricing.Discount => "Discount",
        InvoiceLinePricing.PercentDiscount => "% off",
        var _ => "Rate"
    };

    private decimal UnitMin => _draft.Pricing == InvoiceLinePricing.Discount ? 0.01m : 0m;

    private decimal UnitMax => _draft.Pricing == InvoiceLinePricing.PercentDiscount ? 100m : decimal.MaxValue;

    private decimal UnitStep => _draft.Pricing == InvoiceLinePricing.PercentDiscount ? 0.1m : 1m;

    private static string PricingLabel(InvoiceLinePricing pricing)
        => pricing switch {
            InvoiceLinePricing.Discount => "Fixed discount",
            InvoiceLinePricing.PercentDiscount => "Percent discount",
            var _ => "Amount"
        };

    private static string UnitDisplay(InvoiceLineRecord line)
        => line.Pricing == InvoiceLinePricing.PercentDiscount
            ? $"{line.UnitAmount}%"
            : line.UnitAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private async Task AddManualAsync()
    {
        await Add.InvokeAsync(
            new InvoiceLineRecord {
                Title = _draft.Title,
                Description = _draft.Description,
                Quantity = _draft.Quantity,
                UnitAmount = _draft.UnitAmount,
                Pricing = _draft.Pricing
            });
        _draft.Title = string.Empty;
        _draft.Description = string.Empty;
        _draft.Quantity = 1m;
        _draft.UnitAmount = 0m;
    }

    private static string QtyDisplay(decimal quantity)
        => quantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
