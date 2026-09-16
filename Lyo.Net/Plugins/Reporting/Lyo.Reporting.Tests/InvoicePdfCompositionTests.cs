using Lyo.Billing;
using Lyo.Reporting.Api;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Tests;

public sealed class InvoicePdfCompositionTests
{
    [Fact]
    public void CreateRequest_OmitsTaxRow_WhenTaxAmountIsZero()
    {
        var invoice = SampleInvoice(taxAmount: 0m, total: 100m);
        var req = InvoicePdfComposition.CreateRequest(invoice);
        var report = ReportJson.Deserialize<object>(req.ReportDataJson!);
        var totals = SectionBody.WalkControls(report.Sections[0].Controls).OfType<Block>().Single(b => b.ContentType == ContentType.Totals);
        Assert.Equal(["Subtotal|$100.00", "Total|$100.00"], totals.ListItems);
        Assert.DoesNotContain(totals.ListItems!, r => r.StartsWith("Tax|", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateRequest_IncludesTaxRow_WhenTaxAmountIsPositive()
    {
        var invoice = SampleInvoice(taxAmount: 8m, total: 108m);
        var rows = InvoicePdfComposition.TotalsRows(invoice);
        Assert.Equal(["Subtotal|$100.00", "Tax|$8.00", "Total|$108.00"], rows);
    }

    [Fact]
    public void LineRow_FormatsQtyAndAmount_AndSkipsDuplicateDescription()
    {
        var row = InvoicePdfComposition.LineRow(
            new InvoiceLineRecord { Title = "counties", Description = "counties", Quantity = 6m, Amount = 6600m },
            "USD");
        Assert.Equal("counties", row["title"]);
        Assert.Equal("", row["description"]);
        Assert.Equal("6", row["qty"]);
        Assert.Equal("$6,600.00", row["amount"]);
    }

    [Fact]
    public void LineRow_KeepsDistinctDescription()
    {
        var row = InvoicePdfComposition.LineRow(
            new InvoiceLineRecord { Title = "Counties", Description = "Q3 research", Quantity = 1.5m, Amount = 150m },
            "USD");
        Assert.Equal("Counties", row["title"]);
        Assert.Equal("Q3 research", row["description"]);
        Assert.Equal("1.5", row["qty"]);
        Assert.Equal("$150.00", row["amount"]);
    }

    [Fact]
    public void CreateRequest_DoesNotRepeatDesignerCalloutOrToc()
    {
        var req = InvoicePdfComposition.CreateRequest(SampleInvoice(taxAmount: 0m, total: 100m));
        var report = ReportJson.Deserialize<object>(req.ReportDataJson!);
        Assert.True(string.IsNullOrEmpty(report.Description));
        var blocks = report.Sections.SelectMany(s => SectionBody.WalkControls(s.Controls)).OfType<Block>();
        Assert.DoesNotContain(blocks, b => b.ContentType == ContentType.TableOfContents);
        Assert.Equal("{InvoiceNo}", report.Title);
        Assert.Equal("Invoice", report.Layout?.HeaderText);
    }

    private static InvoiceRecord SampleInvoice(decimal taxAmount, decimal total)
        => new() {
            Number = "INV-2026-00001",
            Currency = "USD",
            Subtotal = 100m,
            TaxAmount = taxAmount,
            Total = total,
            BillToName = "Klein",
            BillToLine1 = "100 Main Street",
            Status = InvoiceStatus.Issued,
            Lines = [new InvoiceLineRecord { Title = "Work", Description = "Work", Quantity = 1m, Amount = 100m }]
        };
}
