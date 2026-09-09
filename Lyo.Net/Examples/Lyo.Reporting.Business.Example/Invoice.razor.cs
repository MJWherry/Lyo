using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Business.Example;

public partial class Invoice
{
    public sealed record InvoiceOptions(Guid InvoiceNumber, string ClientName, string ClientAddress, string ClientCity, string ClientEmail, string CardNumber, DateTime InvoiceTimestamp, DateOnly StartDate, DateOnly EndDate, List<InvoiceLineItem> LineItems, string Status)
    {
        public decimal GrandTotal => LineItems.Sum(i => i.Total);
    }

    public sealed record InvoiceLineItem(string Description, decimal Cost, decimal Quantity, decimal? TotalOverride = null)
    {
        public decimal Total => TotalOverride ?? Cost * Quantity;
    }

    [Parameter]
    [EditorRequired]
    public InvoiceOptions Options { get; set; }
}
