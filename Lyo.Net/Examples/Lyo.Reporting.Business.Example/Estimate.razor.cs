using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Business.Example;

public partial class Estimate
{
    public sealed record EstimateOptions(string EstimateNumber, DateTime IssueDate, DateTime? ValidUntil, string CompanyName, string CompanyAddress, string ClientName, string ClientAddress, List<EstimateLineItem> LineItems, string Terms, string Notes)
    {
        public decimal GrandTotal => LineItems.Sum(i => i.Total);
    }

    public sealed record EstimateLineItem(string Description, int Quantity, decimal UnitPrice)
    {
        public decimal Total => Quantity * UnitPrice;
    }

    [Parameter]
    [EditorRequired]
    public EstimateOptions Options { get; set; } = null!;
}
