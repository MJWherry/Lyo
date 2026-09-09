using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Business.Example;

public partial class InvoiceProfessional
{
    public sealed record InvoiceProfessionalOptions(string InvoiceNumber, DateTime InvoiceDate, DateTime DueDate, string BillerName, string BillerAddress, string BillerEmail, string BillerPhone, string CustomerName, string CustomerAddress, string CustomerEmail, List<InvoiceProfessionalLineItem> LineItems, decimal? TaxRate, decimal? ShippingAmount, string PaymentTerms)
    {
        public decimal Subtotal => LineItems.Sum(i => i.Total);

        public decimal TaxAmount => TaxRate.HasValue ? Subtotal * TaxRate.Value : 0;

        public decimal Shipping => ShippingAmount ?? 0;

        public decimal GrandTotal => Subtotal + TaxAmount + Shipping;
    }

    public sealed record InvoiceProfessionalLineItem(string Description, string? Sku, int Quantity, decimal UnitPrice)
    {
        public decimal Total => Quantity * UnitPrice;
    }

    [Parameter]
    [EditorRequired]
    public InvoiceProfessionalOptions Options { get; set; } = null!;
}
