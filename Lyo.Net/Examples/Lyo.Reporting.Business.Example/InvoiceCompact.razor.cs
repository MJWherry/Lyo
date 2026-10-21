using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Business.Example;

public partial class InvoiceCompact
{
    [Parameter]
    [EditorRequired]
    public InvoiceCompactOptions Options { get; set; } = null!;

    public sealed record InvoiceCompactOptions(
        string InvoiceNumber,
        DateTime InvoiceDate,
        DateTime DueDate,
        string FromCompany,
        string FromAddress,
        string ToName,
        string ToAddress,
        List<InvoiceCompactLineItem> LineItems,
        string PaymentInstructions)
    {
        public decimal GrandTotal => LineItems.Sum(i => i.Total);
    }

    public sealed record InvoiceCompactLineItem(string Description, int Quantity, decimal UnitPrice)
    {
        public decimal Total => Quantity * UnitPrice;
    }
}