using Lyo.EntityReference.Models;
using Lyo.Ledger;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Billing.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class BillingPostgresStoreTests(BillingPostgresFixture fixture)
{
    [Fact]
    public async Task IssueAndPay_PostsArThenCash()
    {
        var ct = TestCt;
        var billing = fixture.ServiceProvider.GetRequiredService<IBillingStore>();
        var ledger = fixture.ServiceProvider.GetRequiredService<ILedgerStore>();
        var client = new BillingClientRecord {
            DisplayName = "Acme",
            InvoicePrefix = "ACME",
            PaymentTermsDays = 30,
            TaxId = "12-3456789",
            DefaultCurrency = "USD",
            ContactName = "Ada Lovelace",
            Line1 = "100 Main",
            City = "Austin",
            Region = "TX",
            PostalCode = "78701",
            Country = "US"
        };

        await billing.SaveClientAsync(client, ct: ct);
        var charge = new ChargeRecord {
            ClientId = client.Id,
            Kind = ChargeKind.Time,
            Description = "Engineering",
            Quantity = 10m,
            UnitAmount = 150m
        };

        await billing.SaveChargeAsync(charge, ct: ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], 0.08m, ct: ct);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Equal(1500m, invoice.Subtotal);
        Assert.Equal(120m, invoice.TaxAmount);
        Assert.Equal(1620m, invoice.Total);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.StartsWith("ACME-", invoice.Number);
        Assert.Equal("Ada Lovelace", invoice.BillToContactName);
        Assert.Equal("100 Main", invoice.BillToLine1);
        Assert.Equal("12-3456789", invoice.TaxId);
        Assert.Equal(1620m, await ledger.GetClientArBalanceAsync(client.Id, ct: ct));
        var billed = await billing.GetChargeByIdAsync(charge.Id, ct: ct);
        Assert.Equal(invoice.Id, billed!.InvoiceId);
        var payment = await billing.RecordPaymentAsync(invoice.Id, 1620m, "USD", PaymentMethod.Wire, "WIRE-4411", ct: ct);
        Assert.NotEqual(Guid.Empty, payment.JournalId);
        invoice = await billing.GetInvoiceByIdAsync(invoice.Id, ct: ct);
        Assert.Equal(InvoiceStatus.Paid, invoice!.Status);
        Assert.Equal(0m, await ledger.GetClientArBalanceAsync(client.Id, ct: ct));
    }

    [Fact]
    public async Task VoidIssued_UnlinksChargesAndClearsAr()
    {
        var ct = TestCt;
        var billing = fixture.ServiceProvider.GetRequiredService<IBillingStore>();
        var ledger = fixture.ServiceProvider.GetRequiredService<ILedgerStore>();
        var client = new BillingClientRecord { DisplayName = "Beta" };
        await billing.SaveClientAsync(client, ct: ct);
        var charge = new ChargeRecord {
            ClientId = client.Id,
            Description = "Work",
            Quantity = 1m,
            UnitAmount = 100m
        };

        await billing.SaveChargeAsync(charge, ct: ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        Assert.Equal(100m, await ledger.GetClientArBalanceAsync(client.Id, ct: ct));
        invoice = await billing.VoidInvoiceAsync(invoice.Id, ct: ct);
        Assert.Equal(InvoiceStatus.Void, invoice.Status);
        var unbilled = await billing.ListUnbilledChargesAsync(client.Id, ct: ct);
        Assert.Single(unbilled);
        Assert.Equal(0m, await ledger.GetClientArBalanceAsync(client.Id, ct: ct));
    }

    [Fact]
    public async Task CreateInvoiceFromCharges_ChargeOnOtherDraft_Throws()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "DupDraft", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 50m, ct);
        await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct));
    }

    [Fact]
    public async Task CreateInvoiceFromCharges_ChargeAlreadyIssued_Throws()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "DupIssued", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 50m, ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct));
    }

    [Fact]
    public async Task RecordPayment_AmountExceedsBalance_Throws()
    {
        var ct = TestCt;
        var (billing, ledger) = Stores();
        var client = await SaveClientAsync(billing, "Overpay", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 100m, ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        await billing.RecordPaymentAsync(invoice.Id, 60m, "USD", PaymentMethod.Wire, "P1", ct: ct);
        await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.RecordPaymentAsync(invoice.Id, 50m, "USD", PaymentMethod.Wire, "P2", ct: ct));
        invoice = await billing.GetInvoiceByIdAsync(invoice.Id, ct: ct);
        Assert.Equal(60m, invoice!.AmountPaid);
        Assert.Equal(40m, await ledger.GetClientArBalanceAsync(client.Id, ct: ct));
    }

    [Fact]
    public async Task RecordPayment_CurrencyMismatch_Throws()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Eur", ct, "EUR");
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 100m, ct, "EUR");
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        Assert.Equal("EUR", invoice.Currency);
        await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.RecordPaymentAsync(invoice.Id, 100m, "USD", PaymentMethod.Wire, "FX", ct: ct));
    }

    [Fact]
    public async Task RecordPayment_NullCurrency_UsesInvoiceCurrency()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "EurNull", ct, "EUR");
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 100m, ct, "EUR");
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        var payment = await billing.RecordPaymentAsync(invoice.Id, 100m, null, PaymentMethod.Wire, "P", ct: ct);
        Assert.Equal("EUR", payment.Currency);
    }

    [Fact]
    public async Task SaveCharge_AlreadyBilled_Throws()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Billed", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 80m, ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        charge.Quantity = 9m;
        await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.SaveChargeAsync(charge, ct: ct));
        var loaded = await billing.GetChargeByIdAsync(charge.Id, ct: ct);
        Assert.Equal(1m, loaded!.Quantity);
    }

    [Fact]
    public async Task IssueInvoice_ZeroTotal_ThrowsMeaningfulBillingException()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Zero", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 0m, ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        var ex = await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.IssueInvoiceAsync(invoice.Id, ct: ct));
        Assert.Contains("total", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IssueInvoice_WithTax_PostsArRevenueTax()
    {
        var ct = TestCt;
        var (billing, ledger) = Stores();
        var client = await SaveClientAsync(billing, "TaxCo", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 10m, 150m, ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], 0.08m, ct: ct);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        var journal = await ledger.GetJournalByIdAsync(invoice.IssueJournalId!.Value, ct: ct);
        Assert.NotNull(journal);
        var ar = await ledger.GetAccountByCodeAsync(LedgerAccountCodes.AccountsReceivable, ct: ct);
        var revenue = await ledger.GetAccountByCodeAsync(LedgerAccountCodes.Revenue, ct: ct);
        var tax = await ledger.GetAccountByCodeAsync(LedgerAccountCodes.TaxPayable, ct: ct);
        Assert.Equal(1620m, journal.Lines.Single(l => l.AccountId == ar!.Id).Debit);
        Assert.Equal(1500m, journal.Lines.Single(l => l.AccountId == revenue!.Id).Credit);
        Assert.Equal(120m, journal.Lines.Single(l => l.AccountId == tax!.Id).Credit);
    }

    [Fact]
    public async Task IssueInvoice_TaxExemptClient_TaxAmountZero()
    {
        var ct = TestCt;
        var (billing, ledger) = Stores();
        var client = new BillingClientRecord { DisplayName = "Exempt", TaxExempt = true, DefaultCurrency = "USD" };
        await billing.SaveClientAsync(client, ct: ct);
        var charge = await SaveChargeAsync(billing, client.Id, 2m, 50m, ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], 0.08m, ct: ct);
        Assert.Equal(0m, invoice.TaxAmount);
        Assert.Equal(invoice.Subtotal, invoice.Total);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        var journal = await ledger.GetJournalByIdAsync(invoice.IssueJournalId!.Value, ct: ct);
        var revenue = await ledger.GetAccountByCodeAsync(LedgerAccountCodes.Revenue, ct: ct);
        Assert.Equal(invoice.Total, journal!.Lines.Single(l => l.AccountId == revenue!.Id).Credit);
        Assert.DoesNotContain(journal.Lines, l => l.Credit == 8m);
    }

    [Fact]
    public async Task VoidInvoice_WithPayment_Throws()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "PaidVoid", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 100m, ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        await billing.RecordPaymentAsync(invoice.Id, 100m, "USD", PaymentMethod.Check, "CK-1", ct: ct);
        await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.VoidInvoiceAsync(invoice.Id, ct: ct));
        invoice = await billing.GetInvoiceByIdAsync(invoice.Id, ct: ct);
        Assert.Equal(InvoiceStatus.Paid, invoice!.Status);
    }

    [Fact]
    public async Task AddInvoiceLine_PercentDiscountOfSubtotal_StoresNegativeAmount()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Pct", ct);
        var draft = await billing.CreateDraftInvoiceAsync(client.Id, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "Work", Quantity = 10m, UnitAmount = 100m }, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "10% off", Pricing = InvoiceLinePricing.PercentDiscount, UnitAmount = 10m }, ct: ct);
        var invoice = await billing.GetInvoiceByIdAsync(draft.Id, ct: ct);
        Assert.Equal(2, invoice!.Lines.Count);
        Assert.Equal("Work", invoice.Lines[0].Title);
        Assert.Equal("Work", invoice.Lines[0].Description);
        Assert.Equal(-100m, invoice.Lines[1].Amount);
        Assert.Equal(1m, invoice.Lines[1].Quantity);
        Assert.Equal(900m, invoice.Subtotal);
    }

    [Fact]
    public async Task AddInvoiceLine_FixedDiscount_StoresNegativeAmount()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Disc", ct);
        var draft = await billing.CreateDraftInvoiceAsync(client.Id, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "Work", Quantity = 1m, UnitAmount = 200m }, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "Courtesy", Pricing = InvoiceLinePricing.Discount, UnitAmount = 50m }, ct: ct);
        var invoice = await billing.GetInvoiceByIdAsync(draft.Id, ct: ct);
        Assert.Equal(-50m, invoice!.Lines[1].Amount);
        Assert.Equal(150m, invoice.Subtotal);
    }

    [Fact]
    public async Task AddInvoiceLine_MixedOrder_RecalcAfterDeletingCharge()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Mix", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 10m, 100m, ct);
        var draft = await billing.CreateDraftInvoiceAsync(client.Id, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { ChargeId = charge.Id }, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "Discount", Pricing = InvoiceLinePricing.Discount, UnitAmount = 50m }, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "10%", Pricing = InvoiceLinePricing.PercentDiscount, UnitAmount = 10m }, ct: ct);
        var invoice = await billing.GetInvoiceByIdAsync(draft.Id, ct: ct);
        Assert.Equal(1000m, invoice!.Lines[0].Amount);
        Assert.Equal(-50m, invoice.Lines[1].Amount);
        Assert.Equal(-95m, invoice.Lines[2].Amount);
        Assert.Equal(855m, invoice.Subtotal);
        await billing.RemoveInvoiceLineAsync(draft.Id, invoice.Lines[0].Id, ct: ct);
        invoice = await billing.GetInvoiceByIdAsync(draft.Id, ct: ct);
        Assert.Equal(-50m, invoice!.Lines[0].Amount);
        Assert.Equal(0m, invoice.Lines[1].Amount);
        Assert.Equal(-50m, invoice.Subtotal);
    }

    [Fact]
    public async Task IssueInvoice_NegativeTotal_Throws()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Neg", ct);
        var draft = await billing.CreateDraftInvoiceAsync(client.Id, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "Work", Quantity = 1m, UnitAmount = 40m }, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "Too much off", Pricing = InvoiceLinePricing.Discount, UnitAmount = 50m }, ct: ct);
        await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.IssueInvoiceAsync(draft.Id, ct: ct));
    }

    [Fact]
    public async Task AttachContactAndAddress_IssueSnapshotPrefersPrimaryBilling()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = new BillingClientRecord {
            DisplayName = "Snap Co",
            DefaultCurrency = "USD",
            ContactName = "Flattened Name",
            Line1 = "1 Old St",
            City = "Austin",
            Region = "TX",
            PostalCode = "78701",
            Country = "US"
        };

        await billing.SaveClientAsync(client, ct: ct);
        var person = await billing.SavePersonAsync(new() { DisplayName = "Ada Lovelace" }, ct: ct);
        await billing.AttachContactAsync(client.Id, EntityRef.For<BillingPersonRecord>(person.Id), BillingContactRole.Billing, "CFO", true, true, ct: ct);
        var address = await billing.SaveAddressAsync(
            new() {
                Line1 = "200 New St",
                City = "Dallas",
                Region = "TX",
                PostalCode = "75201",
                Country = "US"
            }, ct: ct);

        await billing.AttachAddressAsync(client.Id, EntityRef.For<BillingAddressRecord>(address.Id), BillingAddressUsage.Billing, true, ct: ct);
        var listed = await billing.ListContactsAsync(client.Id, ct: ct);
        Assert.Single(listed);
        Assert.Equal(BillingEntityTypes.BillingPerson, listed[0].Contact.EntityType);
        var primary = await billing.GetPrimaryBillingAddressAsync(client.Id, ct: ct);
        Assert.NotNull(primary);
        var draft = await billing.CreateDraftInvoiceAsync(client.Id, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "Work", Quantity = 1m, UnitAmount = 10m }, ct: ct);
        var invoice = await billing.IssueInvoiceAsync(draft.Id, ct: ct);
        Assert.Equal("Ada Lovelace", invoice.BillToContactName);
        Assert.Equal("200 New St", invoice.BillToLine1);
        Assert.Equal("Dallas", invoice.BillToCity);
    }

    [Fact]
    public async Task IssueInvoice_ConcurrentSamePrefix_ProducesDistinctNumbers()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Race", ct, prefix: "RACE");
        var a = await SaveChargeAsync(billing, client.Id, 1m, 10m, ct);
        var b = await SaveChargeAsync(billing, client.Id, 1m, 20m, ct);
        var draftA = await billing.CreateInvoiceFromChargesAsync(client.Id, [a.Id], ct: ct);
        var draftB = await billing.CreateInvoiceFromChargesAsync(client.Id, [b.Id], ct: ct);
        var issued = await Task.WhenAll(billing.IssueInvoiceAsync(draftA.Id, ct: ct), billing.IssueInvoiceAsync(draftB.Id, ct: ct));
        Assert.NotEqual(issued[0].Number, issued[1].Number);
        Assert.All(issued, i => Assert.StartsWith("RACE-", i.Number));
    }

    [Fact]
    public async Task CreateDraftInvoice_AddManualLine_Issues()
    {
        var ct = TestCt;
        var (billing, ledger) = Stores();
        var client = await SaveClientAsync(billing, "Manual", ct);
        var draft = await billing.CreateDraftInvoiceAsync(client.Id, 0.1m, ct: ct);
        Assert.Equal(InvoiceStatus.Draft, draft.Status);
        Assert.Empty(draft.Lines);
        await billing.AddInvoiceLineAsync(draft.Id, new() { Description = "Design", Quantity = 2m, UnitAmount = 100m }, ct: ct);
        var invoice = await billing.GetInvoiceByIdAsync(draft.Id, ct: ct);
        Assert.Equal(200m, invoice!.Subtotal);
        Assert.Equal(20m, invoice.TaxAmount);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal(220m, await ledger.GetClientArBalanceAsync(client.Id, ct: ct));
    }

    [Fact]
    public async Task AddInvoiceLine_ReservesCharge_AndRemoveUnreserves()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Reserve", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 40m, ct);
        var draft = await billing.CreateDraftInvoiceAsync(client.Id, ct: ct);
        await billing.AddInvoiceLineAsync(draft.Id, new() { ChargeId = charge.Id }, ct: ct);
        Assert.Empty(await billing.ListUnbilledChargesAsync(client.Id, ct: ct));
        var reserved = await billing.GetChargeByIdAsync(charge.Id, ct: ct);
        Assert.Equal(draft.Id, reserved!.InvoiceId);
        var invoice = await billing.GetInvoiceByIdAsync(draft.Id, ct: ct);
        await billing.RemoveInvoiceLineAsync(draft.Id, invoice!.Lines[0].Id, ct: ct);
        Assert.Single(await billing.ListUnbilledChargesAsync(client.Id, ct: ct));
    }

    [Fact]
    public async Task DeleteDraftInvoice_UnreservesCharges()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "DelDraft", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 25m, ct);
        var draft = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        await billing.DeleteDraftInvoiceAsync(draft.Id, ct: ct);
        Assert.Null(await billing.GetInvoiceByIdAsync(draft.Id, ct: ct));
        Assert.Single(await billing.ListUnbilledChargesAsync(client.Id, ct: ct));
    }

    [Fact]
    public async Task AddInvoiceLine_OnIssued_Throws()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "IssuedEdit", ct);
        var charge = await SaveChargeAsync(billing, client.Id, 1m, 30m, ct);
        var invoice = await billing.CreateInvoiceFromChargesAsync(client.Id, [charge.Id], ct: ct);
        invoice = await billing.IssueInvoiceAsync(invoice.Id, ct: ct);
        await Assert.ThrowsAsync<BillingInvoiceStateException>(() => billing.AddInvoiceLineAsync(
            invoice.Id, new() { Description = "Extra", Quantity = 1m, UnitAmount = 1m }, ct: ct));
    }

    [Fact]
    public async Task GetBillingSummary_CountsDraftAndIssued()
    {
        var ct = TestCt;
        var (billing, _) = Stores();
        var client = await SaveClientAsync(billing, "Kpis", ct);
        var draftCharge = await SaveChargeAsync(billing, client.Id, 1m, 10m, ct);
        var issuedCharge = await SaveChargeAsync(billing, client.Id, 1m, 50m, ct);
        await billing.CreateInvoiceFromChargesAsync(client.Id, [draftCharge.Id], ct: ct);
        var issued = await billing.CreateInvoiceFromChargesAsync(client.Id, [issuedCharge.Id], ct: ct);
        await billing.IssueInvoiceAsync(issued.Id, ct: ct);
        var summary = await billing.GetBillingSummaryAsync(ct: ct);
        Assert.True(summary.DraftCount >= 1);
        Assert.True(summary.IssuedCount >= 1);
        Assert.True(summary.OutstandingTotal >= 50m);
        Assert.True(summary.UnbilledChargeCount >= 0);
    }

    private (IBillingStore Billing, ILedgerStore Ledger) Stores()
        => (fixture.ServiceProvider.GetRequiredService<IBillingStore>(), fixture.ServiceProvider.GetRequiredService<ILedgerStore>());

    private static async Task<BillingClientRecord> SaveClientAsync(IBillingStore billing, string name, CancellationToken ct, string currency = "USD", string? prefix = null)
    {
        var client = new BillingClientRecord { DisplayName = name, DefaultCurrency = currency, InvoicePrefix = prefix };
        await billing.SaveClientAsync(client, ct: ct);
        return client;
    }

    private static async Task<ChargeRecord> SaveChargeAsync(IBillingStore billing, Guid clientId, decimal qty, decimal unit, CancellationToken ct, string currency = "USD")
    {
        var charge = new ChargeRecord {
            ClientId = clientId,
            Description = "Work",
            Quantity = qty,
            UnitAmount = unit,
            Currency = currency
        };

        await billing.SaveChargeAsync(charge, ct: ct);
        return charge;
    }
}