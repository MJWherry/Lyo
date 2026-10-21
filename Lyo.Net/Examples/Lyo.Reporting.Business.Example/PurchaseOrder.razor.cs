using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Business.Example;

public partial class PurchaseOrder
{
    [Parameter]
    [EditorRequired]
    public PurchaseOrderOptions Options { get; set; } = null!;

    public sealed record PurchaseOrderOptions(
        string PONumber,
        DateTime OrderDate,
        DateTime? RequestedDeliveryDate,
        string BuyerCompany,
        string BuyerAddress,
        string ShipToName,
        string ShipToAddress,
        List<PurchaseOrderLineItem> LineItems,
        string Notes)
    {
        public decimal GrandTotal => LineItems.Sum(i => i.Total);
    }

    public sealed record PurchaseOrderLineItem(string ItemNumber, string Description, int Quantity, decimal UnitPrice)
    {
        public decimal Total => Quantity * UnitPrice;
    }
}