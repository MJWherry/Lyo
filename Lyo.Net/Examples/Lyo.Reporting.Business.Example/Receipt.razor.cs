using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Business.Example;

public partial class Receipt
{
    public sealed record ReceiptOptions(string ReceiptNumber, DateTime TransactionDate, string MerchantName, string MerchantAddress, string CustomerName, string PaymentMethod, List<ReceiptLineItem> Items, decimal TaxAmount, decimal Total);

    public sealed record ReceiptLineItem(string Description, int Quantity, decimal UnitPrice)
    {
        public decimal Total => Quantity * UnitPrice;
    }

    [Parameter]
    [EditorRequired]
    public ReceiptOptions Options { get; set; } = null!;
}
