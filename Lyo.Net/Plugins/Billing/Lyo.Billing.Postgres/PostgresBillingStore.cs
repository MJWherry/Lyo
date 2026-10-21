using System.Data;
using System.Data.Common;
using System.Transactions;
using Lyo.Billing.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.EntityReference.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Ledger;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Lyo.Billing.Postgres;

/// <summary>Postgres-backed <see cref="IBillingStore" />. Posts journals through <see cref="ILedgerStore" />.</summary>
public sealed class PostgresBillingStore : IBillingStore, IHealth
{
    private readonly IDbContextFactory<BillingDbContext> _contextFactory;
    private readonly IBillingFieldEncryptionService? _encryption;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;
    private readonly ILedgerStore _ledger;

    /// <summary>Constructs the store.</summary>
    public PostgresBillingStore(
        IDbContextFactory<BillingDbContext> contextFactory,
        ILedgerStore ledger,
        EntityRefOptions entityRefOptions,
        PostgresBillingOptions billingOptions,
        IBillingFieldEncryptionService? encryption = null)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(ledger);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(billingOptions);
        _contextFactory = contextFactory;
        _ledger = ledger;
        _entityRefOptions = entityRefOptions;
        _featureTenancy = billingOptions.Tenancy;
        _encryption = encryption;
    }

    /// <inheritdoc />
    public async Task SaveClientAsync(BillingClientRecord client, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(client);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(client.DisplayName, nameof(client.DisplayName));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        BillingClientEntity entity;
        if (client.Id != Guid.Empty) {
            var existing = await context.Clients.FirstOrDefaultAsync(c => c.Id == client.Id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            entity = existing ?? new BillingClientEntity { Id = client.Id, CreatedAt = DateTime.UtcNow };
            if (existing == null)
                context.Clients.Add(entity);
        }
        else {
            entity = new() { Id = LyoGuid.CreateV7(), CreatedAt = DateTime.UtcNow };
            context.Clients.Add(entity);
            client.Id = entity.Id;
        }

        entity.TenantId = resolvedTenant;
        entity.DisplayName = client.DisplayName;
        entity.LegalName = client.LegalName;
        entity.Status = (int)client.Status;
        entity.TaxExempt = client.TaxExempt;
        entity.DefaultCurrency = string.IsNullOrWhiteSpace(client.DefaultCurrency) ? "USD" : client.DefaultCurrency;
        entity.PaymentTermsDays = client.PaymentTermsDays <= 0 ? 30 : client.PaymentTermsDays;
        entity.InvoicePrefix = client.InvoicePrefix;
        entity.DefaultInvoiceNotes = client.DefaultInvoiceNotes;
        entity.ContactName = client.ContactName;
        entity.Line1 = client.Line1;
        entity.Line2 = client.Line2;
        entity.City = client.City;
        entity.Region = client.Region;
        entity.PostalCode = client.PostalCode;
        entity.Country = client.Country;
        if (!string.IsNullOrWhiteSpace(client.TaxId)) {
            entity.TaxIdLast4 = BillingTaxId.Last4(client.TaxId);
            EncryptSecret(client.TaxId, v => entity.TaxId = v, encrypted => entity.EncryptedTaxId = encrypted);
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        client.TenantId = entity.TenantId;
        client.CreatedAt = entity.CreatedAt;
        client.TaxIdLast4 = entity.TaxIdLast4;
    }

    /// <inheritdoc />
    public async Task<BillingClientRecord?> GetClientByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return entity == null ? null : ToClient(entity);
    }

    /// <inheritdoc />
    public async Task SaveChargeAsync(ChargeRecord charge, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(charge);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(charge.Description, nameof(charge.Description));
        if (charge.ClientId == Guid.Empty)
            throw new ArgumentException("Charge requires a client.", nameof(charge));

        if (charge.Quantity <= 0)
            throw new ArgumentException("Charge quantity must be positive.", nameof(charge));

        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ChargeEntity entity;
        if (charge.Id != Guid.Empty) {
            var existing = await context.Charges.FirstOrDefaultAsync(c => c.Id == charge.Id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (existing?.InvoiceId != null)
                throw new BillingInvoiceStateException("A billed charge cannot be edited.");

            entity = existing ?? new ChargeEntity { Id = charge.Id, CreatedAt = DateTime.UtcNow };
            if (existing == null)
                context.Charges.Add(entity);
        }
        else {
            entity = new() { Id = LyoGuid.CreateV7(), CreatedAt = DateTime.UtcNow };
            context.Charges.Add(entity);
            charge.Id = entity.Id;
        }

        entity.TenantId = resolvedTenant;
        entity.ClientId = charge.ClientId;
        entity.Kind = (int)charge.Kind;
        entity.Description = charge.Description;
        entity.Quantity = charge.Quantity;
        entity.UnitAmount = charge.UnitAmount;
        entity.Currency = string.IsNullOrWhiteSpace(charge.Currency) ? "USD" : charge.Currency;
        entity.OccurredAt = charge.OccurredAt;
        entity.CreatedByEntityType = charge.CreatedBy?.EntityType;
        entity.CreatedByEntityId = charge.CreatedBy?.EntityId;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        charge.TenantId = entity.TenantId;
        charge.CreatedAt = entity.CreatedAt;
        charge.InvoiceId = entity.InvoiceId;
    }

    /// <inheritdoc />
    public async Task<ChargeRecord?> GetChargeByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Charges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return entity == null ? null : ToCharge(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChargeRecord>> ListUnbilledChargesAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Charges.AsNoTracking()
            .Where(c => c.ClientId == clientId && c.TenantId == resolvedTenant && c.InvoiceId == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(ToCharge).ToList();
    }

    /// <inheritdoc />
    public async Task<InvoiceRecord> CreateInvoiceFromChargesAsync(
        Guid clientId,
        IReadOnlyList<Guid> chargeIds,
        decimal taxRate = 0m,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chargeIds);
        if (chargeIds.Count == 0)
            throw new ArgumentException("At least one charge is required.", nameof(chargeIds));

        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var client = await RequireClientAsync(context, clientId, resolvedTenant, ct).ConfigureAwait(false);
        var charges = await context.Charges.Where(c => chargeIds.Contains(c.Id) && c.TenantId == resolvedTenant && c.ClientId == clientId).ToListAsync(ct).ConfigureAwait(false);
        if (charges.Count != chargeIds.Count)
            throw new InvalidOperationException("One or more charges were not found for this client.");

        if (charges.Any(c => c.InvoiceId != null))
            throw new BillingInvoiceStateException("A selected charge is already on an invoice.");

        var invoice = new InvoiceEntity {
            Id = LyoGuid.CreateV7(),
            TenantId = resolvedTenant,
            ClientId = clientId,
            Status = (int)InvoiceStatus.Draft,
            Currency = client.DefaultCurrency,
            TaxRate = taxRate,
            Notes = client.DefaultInvoiceNotes,
            CreatedAt = DateTime.UtcNow
        };

        var n = 1;
        foreach (var charge in charges.OrderBy(c => c.CreatedAt)) {
            var amount = decimal.Round(charge.Quantity * charge.UnitAmount, 4, MidpointRounding.AwayFromZero);
            invoice.Lines.Add(
                new() {
                    Id = LyoGuid.CreateV7(),
                    ChargeId = charge.Id,
                    LineNumber = n++,
                    Title = TruncateTitle(charge.Description),
                    Description = charge.Description,
                    Quantity = charge.Quantity,
                    UnitAmount = charge.UnitAmount,
                    Amount = amount,
                    Pricing = (int)InvoiceLinePricing.Amount
                });
        }

        RecalcTotals(invoice, client.TaxExempt);
        foreach (var charge in charges)
            charge.InvoiceId = invoice.Id;

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return await LoadInvoiceAsync(context, invoice.Id, resolvedTenant, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Draft invoice was not saved.");
    }

    /// <inheritdoc />
    public async Task<InvoiceRecord> CreateDraftInvoiceAsync(Guid clientId, decimal taxRate = 0m, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var client = await RequireClientAsync(context, clientId, resolvedTenant, ct).ConfigureAwait(false);
        var invoice = new InvoiceEntity {
            Id = LyoGuid.CreateV7(),
            TenantId = resolvedTenant,
            ClientId = clientId,
            Status = (int)InvoiceStatus.Draft,
            Currency = client.DefaultCurrency,
            TaxRate = taxRate,
            Notes = client.DefaultInvoiceNotes,
            CreatedAt = DateTime.UtcNow
        };

        RecalcTotals(invoice, client.TaxExempt);
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return await LoadInvoiceAsync(context, invoice.Id, resolvedTenant, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Draft invoice was not saved.");
    }

    /// <inheritdoc />
    public async Task<InvoiceLineRecord> AddInvoiceLineAsync(Guid invoiceId, InvoiceLineRecord line, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(line);
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == resolvedTenant, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Invoice '{invoiceId}' was not found.");

        if (invoice.Status != (int)InvoiceStatus.Draft)
            throw new BillingInvoiceStateException("Only a draft invoice can be edited.");

        var client = await RequireClientAsync(context, invoice.ClientId, resolvedTenant, ct).ConfigureAwait(false);
        var chargeId = line.ChargeId;
        var title = line.Title;
        var description = line.Description;
        var quantity = line.Quantity;
        var unitAmount = line.UnitAmount;
        var pricing = line.Pricing;
        if (chargeId is { } cid) {
            var charge = await context.Charges.FirstOrDefaultAsync(c => c.Id == cid && c.TenantId == resolvedTenant && c.ClientId == invoice.ClientId, ct).ConfigureAwait(false) ??
                throw new InvalidOperationException($"Charge '{cid}' was not found for this client.");

            if (charge.InvoiceId != null)
                throw new BillingInvoiceStateException("A selected charge is already on an invoice.");

            if (string.IsNullOrWhiteSpace(title))
                title = charge.Description;

            if (string.IsNullOrWhiteSpace(description))
                description = charge.Description;

            if (quantity == 0)
                quantity = charge.Quantity;

            if (unitAmount == 0)
                unitAmount = charge.UnitAmount;

            pricing = InvoiceLinePricing.Amount;
            charge.InvoiceId = invoice.Id;
        }

        if (string.IsNullOrWhiteSpace(title))
            title = description;

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(title, nameof(line.Title));
        title = TruncateTitle(title);
        InvoiceLinePricingMath.Validate(pricing, quantity, unitAmount);
        quantity = InvoiceLinePricingMath.NormalizedQuantity(pricing, quantity);
        var nextNumber = await context.InvoiceLines.AsNoTracking().Where(l => l.InvoiceId == invoice.Id).Select(l => (int?)l.LineNumber).MaxAsync(ct).ConfigureAwait(false);
        var entity = new InvoiceLineEntity {
            Id = line.Id == Guid.Empty ? LyoGuid.CreateV7() : line.Id,
            InvoiceId = invoice.Id,
            ChargeId = chargeId,
            LineNumber = (nextNumber ?? 0) + 1,
            Title = title,
            Description = description ?? string.Empty,
            Quantity = quantity,
            UnitAmount = unitAmount,
            Pricing = (int)pricing,
            Amount = 0m
        };

        context.InvoiceLines.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalcDraftTotalsAsync(context, invoice.Id, invoice.TaxRate, client.TaxExempt, resolvedTenant, ct).ConfigureAwait(false);
        await context.Entry(entity).ReloadAsync(ct).ConfigureAwait(false);
        return ToLine(entity);
    }

    /// <inheritdoc />
    public async Task RemoveInvoiceLineAsync(Guid invoiceId, Guid lineId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invoice = await RequireDraftInvoiceAsync(context, invoiceId, resolvedTenant, ct).ConfigureAwait(false);
        var line = invoice.Lines.FirstOrDefault(l => l.Id == lineId) ?? throw new InvalidOperationException($"Invoice line '{lineId}' was not found.");
        if (line.ChargeId is { } chargeId) {
            var charge = await context.Charges.FirstOrDefaultAsync(c => c.Id == chargeId && c.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            if (charge != null && charge.InvoiceId == invoice.Id)
                charge.InvoiceId = null;
        }

        context.InvoiceLines.Remove(line);
        invoice.Lines.Remove(line);
        var n = 1;
        foreach (var remaining in invoice.Lines.OrderBy(l => l.LineNumber))
            remaining.LineNumber = n++;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        var client = await RequireClientAsync(context, invoice.ClientId, resolvedTenant, ct).ConfigureAwait(false);
        await RecalcDraftTotalsAsync(context, invoice.Id, invoice.TaxRate, client.TaxExempt, resolvedTenant, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteDraftInvoiceAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invoice = await RequireDraftInvoiceAsync(context, invoiceId, resolvedTenant, ct).ConfigureAwait(false);
        var charges = await context.Charges.Where(c => c.InvoiceId == invoice.Id).ToListAsync(ct).ConfigureAwait(false);
        foreach (var charge in charges)
            charge.InvoiceId = null;

        context.Invoices.Remove(invoice);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BillingSummaryRecord> GetBillingSummaryAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invoices = await context.Invoices.AsNoTracking().Where(i => i.TenantId == resolvedTenant).ToListAsync(ct).ConfigureAwait(false);
        var unbilled = await context.Charges.AsNoTracking().Where(c => c.TenantId == resolvedTenant && c.InvoiceId == null).ToListAsync(ct).ConfigureAwait(false);
        return new() {
            DraftCount = invoices.Count(i => i.Status == (int)InvoiceStatus.Draft),
            IssuedCount = invoices.Count(i => i.Status == (int)InvoiceStatus.Issued),
            PartiallyPaidCount = invoices.Count(i => i.Status == (int)InvoiceStatus.PartiallyPaid),
            PaidCount = invoices.Count(i => i.Status == (int)InvoiceStatus.Paid),
            VoidCount = invoices.Count(i => i.Status == (int)InvoiceStatus.Void),
            OutstandingTotal = invoices.Where(i => i.Status is (int)InvoiceStatus.Issued or (int)InvoiceStatus.PartiallyPaid).Sum(i => i.Total - i.AmountPaid),
            PaidTotal = invoices.Where(i => i.Status != (int)InvoiceStatus.Void).Sum(i => i.AmountPaid),
            UnbilledChargeCount = unbilled.Count,
            UnbilledChargeTotal = unbilled.Sum(c => decimal.Round(c.Quantity * c.UnitAmount, 4, MidpointRounding.AwayFromZero))
        };
    }

    /// <inheritdoc />
    public async Task<InvoiceRecord?> GetInvoiceByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await LoadInvoiceAsync(context, id, resolvedTenant, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceRecord> IssueInvoiceAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        using var scope = BeginReadCommittedScope();
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invoice = await context.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == resolvedTenant, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Invoice '{invoiceId}' was not found.");

        if (invoice.Status != (int)InvoiceStatus.Draft)
            throw new BillingInvoiceStateException("Only a draft invoice can be issued.");

        if (invoice.Lines.Count == 0)
            throw new BillingInvoiceStateException("An invoice must have lines before it is issued.");

        var client = await RequireClientAsync(context, invoice.ClientId, resolvedTenant, ct).ConfigureAwait(false);
        await SnapshotBillToAsync(context, invoice, client, resolvedTenant, ct).ConfigureAwait(false);
        ApplyLineAmounts(invoice.Lines);
        RecalcTotals(invoice, client.TaxExempt);
        if (invoice.Total <= 0)
            throw new BillingInvoiceStateException("An invoice total must be positive before it is issued.");

        var year = DateTime.UtcNow.Year;
        var seqValue = await AllocateInvoiceNumberAsync(resolvedTenant, year, ct).ConfigureAwait(false);
        var prefix = string.IsNullOrWhiteSpace(client.InvoicePrefix) ? "INV" : client.InvoicePrefix.Trim();
        invoice.Number = $"{prefix}-{year}-{seqValue:D5}";
        invoice.Status = (int)InvoiceStatus.Issued;
        invoice.IssuedAt = DateTime.UtcNow;
        invoice.DueAt = invoice.IssuedAt.Value.AddDays(client.PaymentTermsDays);
        var chargeIds = invoice.Lines.Where(l => l.ChargeId != null).Select(l => l.ChargeId!.Value).ToList();
        if (chargeIds.Count > 0) {
            var claimed = await context.Charges
                .Where(c => chargeIds.Contains(c.Id) && c.TenantId == resolvedTenant && c.ClientId == invoice.ClientId && (c.InvoiceId == null || c.InvoiceId == invoice.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (claimed.Count != chargeIds.Count)
                throw new BillingInvoiceStateException("A selected charge is already on an invoice.");

            foreach (var charge in claimed)
                charge.InvoiceId = invoice.Id;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await _ledger.EnsureChartOfAccountsAsync(resolvedTenant, ct).ConfigureAwait(false);
        var ar = await RequireAccountAsync(LedgerAccountCodes.AccountsReceivable, resolvedTenant, ct).ConfigureAwait(false);
        var revenue = await RequireAccountAsync(LedgerAccountCodes.Revenue, resolvedTenant, ct).ConfigureAwait(false);
        var tax = await RequireAccountAsync(LedgerAccountCodes.TaxPayable, resolvedTenant, ct).ConfigureAwait(false);
        var lines = new List<LedgerJournalLineRecord> {
            new() {
                AccountId = ar.Id,
                Debit = invoice.Total,
                ClientId = invoice.ClientId,
                Memo = invoice.Number
            }
        };

        if (invoice.TaxAmount > 0) {
            lines.Add(new() { AccountId = revenue.Id, Credit = invoice.Subtotal, Memo = invoice.Number });
            lines.Add(new() { AccountId = tax.Id, Credit = invoice.TaxAmount, Memo = invoice.Number });
        }
        else
            lines.Add(new() { AccountId = revenue.Id, Credit = invoice.Total, Memo = invoice.Number });

        var journal = await _ledger.PostJournalAsync(new() { Memo = $"Issue {invoice.Number}", Source = EntityRef.For<InvoiceRecord>(invoice.Id) }, lines, resolvedTenant, ct)
            .ConfigureAwait(false);

        invoice.IssueJournalId = journal.Id;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        var issued = await LoadInvoiceAsync(context, invoice.Id, resolvedTenant, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Issued invoice was not saved.");
        scope.Complete();
        return issued;
    }

    /// <inheritdoc />
    public async Task<InvoiceRecord> VoidInvoiceAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        using var scope = BeginReadCommittedScope();
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invoice = await context.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == resolvedTenant, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Invoice '{invoiceId}' was not found.");

        if (invoice.Status is not (int)InvoiceStatus.Issued and not (int)InvoiceStatus.PartiallyPaid and not (int)InvoiceStatus.Paid)
            throw new BillingInvoiceStateException("Only an issued invoice can be voided.");

        if (invoice.AmountPaid != 0)
            throw new BillingInvoiceStateException("An invoice with payments cannot be voided.");

        if (invoice.IssueJournalId is { } journalId)
            await _ledger.VoidJournalAsync(journalId, resolvedTenant, ct).ConfigureAwait(false);

        var charges = await context.Charges.Where(c => c.InvoiceId == invoice.Id).ToListAsync(ct).ConfigureAwait(false);
        foreach (var charge in charges)
            charge.InvoiceId = null;

        invoice.Status = (int)InvoiceStatus.Void;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        var voided = await LoadInvoiceAsync(context, invoice.Id, resolvedTenant, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Voided invoice was not saved.");
        scope.Complete();
        return voided;
    }

    /// <inheritdoc />
    public async Task<PaymentRecord> RecordPaymentAsync(
        Guid invoiceId,
        decimal amount,
        string? currency,
        PaymentMethod method,
        string? reference,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive.", nameof(amount));

        var resolvedTenant = ResolveTenant(tenantId);
        using var scope = BeginReadCommittedScope();
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == resolvedTenant, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Invoice '{invoiceId}' was not found.");

        if (invoice.Status is not (int)InvoiceStatus.Issued and not (int)InvoiceStatus.PartiallyPaid)
            throw new BillingInvoiceStateException("Payments can only be recorded against an issued or partially paid invoice.");

        var remaining = invoice.Total - invoice.AmountPaid;
        if (amount > remaining)
            throw new BillingInvoiceStateException("Payment amount exceeds the remaining balance.");

        var resolvedCurrency = string.IsNullOrWhiteSpace(currency) ? invoice.Currency : currency.Trim();
        if (!string.Equals(resolvedCurrency, invoice.Currency, StringComparison.OrdinalIgnoreCase))
            throw new BillingInvoiceStateException("Payment currency must match the invoice currency.");

        await _ledger.EnsureChartOfAccountsAsync(resolvedTenant, ct).ConfigureAwait(false);
        var cash = await RequireAccountAsync(LedgerAccountCodes.Cash, resolvedTenant, ct).ConfigureAwait(false);
        var ar = await RequireAccountAsync(LedgerAccountCodes.AccountsReceivable, resolvedTenant, ct).ConfigureAwait(false);
        var journal = await _ledger.PostJournalAsync(
                new() { Memo = $"Payment {invoice.Number}", Source = EntityRef.For<InvoiceRecord>(invoice.Id) },
                [
                    new() { AccountId = cash.Id, Debit = amount, Memo = reference }, new() {
                        AccountId = ar.Id,
                        Credit = amount,
                        ClientId = invoice.ClientId,
                        Memo = invoice.Number
                    }
                ], resolvedTenant, ct)
            .ConfigureAwait(false);

        var payment = new PaymentEntity {
            Id = LyoGuid.CreateV7(),
            TenantId = resolvedTenant,
            InvoiceId = invoice.Id,
            Amount = amount,
            Currency = resolvedCurrency,
            Method = (int)method,
            Reference = reference,
            JournalId = journal.Id,
            ReceivedAt = DateTime.UtcNow
        };

        context.Payments.Add(payment);
        invoice.AmountPaid += amount;
        invoice.Status = invoice.AmountPaid >= invoice.Total ? (int)InvoiceStatus.Paid : (int)InvoiceStatus.PartiallyPaid;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        scope.Complete();
        return ToPayment(payment);
    }

    /// <inheritdoc />
    public async Task<PaymentRecord?> GetPaymentByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return entity == null ? null : ToPayment(entity);
    }

    /// <inheritdoc />
    public async Task SaveBillerProfileAsync(BillerProfileRecord profile, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(profile);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(profile.DisplayName, nameof(profile.DisplayName));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.BillerProfiles.FirstOrDefaultAsync(p => p.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        if (entity == null) {
            entity = new() { Id = profile.Id == Guid.Empty ? LyoGuid.CreateV7() : profile.Id, TenantId = resolvedTenant };
            context.BillerProfiles.Add(entity);
        }

        entity.DisplayName = profile.DisplayName;
        entity.LegalName = profile.LegalName;
        EncryptSecret(profile.Remittance, v => entity.Remittance = v, encrypted => entity.EncryptedRemittance = encrypted);
        if (!string.IsNullOrWhiteSpace(profile.BankAccountNumber)) {
            entity.BankAccountLast4 = BillingTaxId.Last4(profile.BankAccountNumber);
            EncryptSecret(profile.BankAccountNumber, v => entity.BankAccountNumber = v, encrypted => entity.EncryptedBankAccountNumber = encrypted);
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        profile.Id = entity.Id;
        profile.TenantId = entity.TenantId;
        profile.BankAccountLast4 = entity.BankAccountLast4;
    }

    /// <inheritdoc />
    public async Task<BillerProfileRecord?> GetBillerProfileAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.BillerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return entity == null
            ? null
            : new BillerProfileRecord {
                Id = entity.Id,
                TenantId = entity.TenantId,
                DisplayName = entity.DisplayName,
                LegalName = entity.LegalName,
                Remittance = _encryption?.Decrypt(entity.EncryptedRemittance),
                BankAccountNumber = _encryption?.Decrypt(entity.EncryptedBankAccountNumber),
                BankAccountLast4 = entity.BankAccountLast4
            };
    }

    /// <inheritdoc />
    public Task<BillingPersonRecord> SavePersonAsync(BillingPersonRecord person, Guid? tenantId = null, CancellationToken ct = default)
        => SavePayloadAsync(
            person, tenantId, ct, c => c.People, p => p.Id, (p, id) => p.Id = id, p => ArgumentHelpers.ThrowIfNullOrWhiteSpace(p.DisplayName, nameof(p.DisplayName)), (r, e) => {
                e.DisplayName = r.DisplayName;
                e.GivenName = r.GivenName;
                e.FamilyName = r.FamilyName;
                e.Title = r.Title;
            }, e => new() {
                Id = e.Id,
                TenantId = e.TenantId,
                DisplayName = e.DisplayName,
                GivenName = e.GivenName,
                FamilyName = e.FamilyName,
                Title = e.Title,
                CreatedTimestamp = e.CreatedTimestamp
            });

    /// <inheritdoc />
    public Task<BillingPersonRecord?> GetPersonByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
        => GetPayloadAsync(
            id, tenantId, ct, c => c.People, e => new BillingPersonRecord {
                Id = e.Id,
                TenantId = e.TenantId,
                DisplayName = e.DisplayName,
                GivenName = e.GivenName,
                FamilyName = e.FamilyName,
                Title = e.Title,
                CreatedTimestamp = e.CreatedTimestamp
            });

    /// <inheritdoc />
    public Task<BillingPhoneNumberRecord> SavePhoneNumberAsync(BillingPhoneNumberRecord phone, Guid? tenantId = null, CancellationToken ct = default)
        => SavePayloadAsync(
            phone, tenantId, ct, c => c.PhoneNumbers, p => p.Id, (p, id) => p.Id = id, p => ArgumentHelpers.ThrowIfNullOrWhiteSpace(p.Number, nameof(p.Number)), (r, e) => {
                e.Number = r.Number;
                e.Country = r.Country;
            }, e => new() {
                Id = e.Id,
                TenantId = e.TenantId,
                Number = e.Number,
                Country = e.Country,
                CreatedTimestamp = e.CreatedTimestamp
            });

    /// <inheritdoc />
    public Task<BillingPhoneNumberRecord?> GetPhoneNumberByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
        => GetPayloadAsync(
            id, tenantId, ct, c => c.PhoneNumbers, e => new BillingPhoneNumberRecord {
                Id = e.Id,
                TenantId = e.TenantId,
                Number = e.Number,
                Country = e.Country,
                CreatedTimestamp = e.CreatedTimestamp
            });

    /// <inheritdoc />
    public Task<BillingEmailAddressRecord> SaveEmailAddressAsync(BillingEmailAddressRecord email, Guid? tenantId = null, CancellationToken ct = default)
        => SavePayloadAsync(
            email, tenantId, ct, c => c.EmailAddresses, p => p.Id, (p, id) => p.Id = id, p => ArgumentHelpers.ThrowIfNullOrWhiteSpace(p.Email, nameof(p.Email)),
            (r, e) => e.Email = r.Email, e => new() {
                Id = e.Id,
                TenantId = e.TenantId,
                Email = e.Email,
                CreatedTimestamp = e.CreatedTimestamp
            });

    /// <inheritdoc />
    public Task<BillingEmailAddressRecord?> GetEmailAddressByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
        => GetPayloadAsync(
            id, tenantId, ct, c => c.EmailAddresses, e => new BillingEmailAddressRecord {
                Id = e.Id,
                TenantId = e.TenantId,
                Email = e.Email,
                CreatedTimestamp = e.CreatedTimestamp
            });

    /// <inheritdoc />
    public Task<BillingAddressRecord> SaveAddressAsync(BillingAddressRecord address, Guid? tenantId = null, CancellationToken ct = default)
        => SavePayloadAsync(
            address, tenantId, ct, c => c.AddressPayloads, p => p.Id, (p, id) => p.Id = id, _ => { }, (r, e) => {
                e.Line1 = r.Line1;
                e.Line2 = r.Line2;
                e.City = r.City;
                e.Region = r.Region;
                e.PostalCode = r.PostalCode;
                e.Country = r.Country;
            }, e => new() {
                Id = e.Id,
                TenantId = e.TenantId,
                Line1 = e.Line1,
                Line2 = e.Line2,
                City = e.City,
                Region = e.Region,
                PostalCode = e.PostalCode,
                Country = e.Country,
                CreatedTimestamp = e.CreatedTimestamp
            });

    /// <inheritdoc />
    public Task<BillingAddressRecord?> GetAddressByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
        => GetPayloadAsync(
            id, tenantId, ct, c => c.AddressPayloads, e => new BillingAddressRecord {
                Id = e.Id,
                TenantId = e.TenantId,
                Line1 = e.Line1,
                Line2 = e.Line2,
                City = e.City,
                Region = e.Region,
                PostalCode = e.PostalCode,
                Country = e.Country,
                CreatedTimestamp = e.CreatedTimestamp
            });

    /// <inheritdoc />
    public async Task<BillingContactRecord> AttachContactAsync(
        Guid clientId,
        EntityRef contact,
        BillingContactRole role = BillingContactRole.Other,
        string? title = null,
        bool isPrimary = false,
        bool isBillingContact = false,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(contact);
        var resolvedTenant = ResolveTenant(tenantId);
        var relationTenant = resolvedTenant ?? Guid.Empty;
        var forId = EntityRefPersistedGuid.PersistedEntityId(contact);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await EnsureClientAsync(context, clientId, resolvedTenant, ct).ConfigureAwait(false);
        var existing = await context.Contacts.WhereActive()
            .WhereTenant(relationTenant)
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.SubjectEntityType == contact.EntityType && c.SubjectEntityId == forId, ct)
            .ConfigureAwait(false);

        if (existing != null) {
            existing.Role = (int)role;
            existing.Title = title;
            existing.IsPrimary = isPrimary;
            existing.IsBillingContact = isBillingContact;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            return ToContact(existing);
        }

        var actor = EntityRef.ForGuid(EntityRefWellKnown.SystemActorType, EntityRefWellKnown.SystemActorId);
        var entity = new BillingContactEntity {
            Id = LyoGuid.CreateV7(),
            ClientId = clientId,
            SubjectEntityType = contact.EntityType,
            SubjectEntityId = forId,
            ActorEntityType = actor.EntityType,
            ActorEntityId = EntityRefPersistedGuid.PersistedEntityId(actor),
            TenantId = relationTenant,
            Role = (int)role,
            Title = title,
            IsPrimary = isPrimary,
            IsBillingContact = isBillingContact,
            CreatedAt = DateTime.UtcNow,
            Visibility = EntityRefVisibility.Private
        };

        context.Contacts.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToContact(entity);
    }

    /// <inheritdoc />
    public Task DetachContactAsync(Guid linkId, Guid? tenantId = null, CancellationToken ct = default) => SoftDeleteAsync(linkId, tenantId, ct, c => c.Contacts);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BillingContactRecord>> ListContactsAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var relationTenant = ResolveTenant(tenantId) ?? Guid.Empty;
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Contacts.AsNoTracking().WhereActive().WhereTenant(relationTenant).Where(c => c.ClientId == clientId).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToContact).ToList();
    }

    /// <inheritdoc />
    public async Task<BillingContactRecord?> GetPrimaryBillingContactAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var relationTenant = ResolveTenant(tenantId) ?? Guid.Empty;
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Contacts.AsNoTracking()
            .WhereActive()
            .WhereTenant(relationTenant)
            .Where(c => c.ClientId == clientId && c.IsBillingContact)
            .OrderByDescending(c => c.IsPrimary)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return row == null ? null : ToContact(row);
    }

    /// <inheritdoc />
    public Task<BillingPhoneLinkRecord> AttachPhoneAsync(
        Guid clientId,
        EntityRef phone,
        BillingPhoneUsage usage = BillingPhoneUsage.Other,
        bool isPrimary = false,
        EntityRef? contact = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
        => AttachUsageLinkAsync(clientId, phone, (int)usage, isPrimary, contact, tenantId, ct, c => c.Phones, (e, u, p, ctRef) => ApplyUsage(e, u, p, ctRef), ToPhoneLink);

    /// <inheritdoc />
    public Task DetachPhoneAsync(Guid linkId, Guid? tenantId = null, CancellationToken ct = default) => SoftDeleteAsync(linkId, tenantId, ct, c => c.Phones);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BillingPhoneLinkRecord>> ListPhonesAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var relationTenant = ResolveTenant(tenantId) ?? Guid.Empty;
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Phones.AsNoTracking().WhereActive().WhereTenant(relationTenant).Where(c => c.ClientId == clientId).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToPhoneLink).ToList();
    }

    /// <inheritdoc />
    public Task<BillingEmailLinkRecord> AttachEmailAsync(
        Guid clientId,
        EntityRef email,
        BillingEmailUsage usage = BillingEmailUsage.Other,
        bool isPrimary = false,
        EntityRef? contact = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
        => AttachUsageLinkAsync(clientId, email, (int)usage, isPrimary, contact, tenantId, ct, c => c.Emails, (e, u, p, ctRef) => ApplyUsage(e, u, p, ctRef), ToEmailLink);

    /// <inheritdoc />
    public Task DetachEmailAsync(Guid linkId, Guid? tenantId = null, CancellationToken ct = default) => SoftDeleteAsync(linkId, tenantId, ct, c => c.Emails);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BillingEmailLinkRecord>> ListEmailsAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var relationTenant = ResolveTenant(tenantId) ?? Guid.Empty;
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Emails.AsNoTracking().WhereActive().WhereTenant(relationTenant).Where(c => c.ClientId == clientId).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToEmailLink).ToList();
    }

    /// <inheritdoc />
    public Task<BillingAddressLinkRecord> AttachAddressAsync(
        Guid clientId,
        EntityRef address,
        BillingAddressUsage usage = BillingAddressUsage.Other,
        bool isPrimary = false,
        EntityRef? contact = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
        => AttachUsageLinkAsync(clientId, address, (int)usage, isPrimary, contact, tenantId, ct, c => c.Addresses, (e, u, p, ctRef) => ApplyUsage(e, u, p, ctRef), ToAddressLink);

    /// <inheritdoc />
    public Task DetachAddressAsync(Guid linkId, Guid? tenantId = null, CancellationToken ct = default) => SoftDeleteAsync(linkId, tenantId, ct, c => c.Addresses);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BillingAddressLinkRecord>> ListAddressesAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var relationTenant = ResolveTenant(tenantId) ?? Guid.Empty;
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Addresses.AsNoTracking().WhereActive().WhereTenant(relationTenant).Where(c => c.ClientId == clientId).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToAddressLink).ToList();
    }

    /// <inheritdoc />
    public async Task<BillingAddressLinkRecord?> GetPrimaryBillingAddressAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var relationTenant = ResolveTenant(tenantId) ?? Guid.Empty;
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Addresses.AsNoTracking()
            .WhereActive()
            .WhereTenant(relationTenant)
            .Where(c => c.ClientId == clientId && c.Usage == (int)BillingAddressUsage.Billing)
            .OrderByDescending(c => c.IsPrimary)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return row == null ? null : ToAddressLink(row);
    }

    /// <inheritdoc />
    public string HealthCheckName => "billing-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => PostgresHealth.CheckAsync(_contextFactory, PostgresBillingOptions.Schema, ct);

    private static TransactionScope BeginReadCommittedScope()
        => new(
            TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TransactionManager.DefaultTimeout },
            TransactionScopeAsyncFlowOption.Enabled);

    private Guid? ResolveTenant(Guid? tenantId) => TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);

    private static async Task RecalcDraftTotalsAsync(BillingDbContext context, Guid invoiceId, decimal taxRate, bool taxExempt, Guid? tenantId, CancellationToken ct)
    {
        var lines = await context.InvoiceLines.Where(l => l.InvoiceId == invoiceId).OrderBy(l => l.LineNumber).ToListAsync(ct).ConfigureAwait(false);
        ApplyLineAmounts(lines);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        var subtotal = lines.Sum(l => l.Amount);
        var taxAmount = taxExempt || taxRate == 0 ? 0m : decimal.Round(subtotal * taxRate, 4, MidpointRounding.AwayFromZero);
        var total = subtotal + taxAmount;
        var updated = await context.Invoices.Where(i => i.Id == invoiceId && i.TenantId == tenantId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Subtotal, subtotal).SetProperty(i => i.TaxAmount, taxAmount).SetProperty(i => i.Total, total), ct)
            .ConfigureAwait(false);

        if (updated != 1)
            throw new InvalidOperationException($"Invoice '{invoiceId}' totals could not be updated.");
    }

    private static async Task<InvoiceEntity> RequireDraftInvoiceAsync(BillingDbContext context, Guid invoiceId, Guid? tenantId, CancellationToken ct)
    {
        var invoice = await context.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Invoice '{invoiceId}' was not found.");

        if (invoice.Status != (int)InvoiceStatus.Draft)
            throw new BillingInvoiceStateException("Only a draft invoice can be edited.");

        return invoice;
    }

    private async Task<int> AllocateInvoiceNumberAsync(Guid? tenantId, int year, CancellationToken ct)
    {
        var seqTenant = tenantId ?? Guid.Empty;
        using var suppress = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO billing.invoice_sequence (id, tenant_id, year, last_value)
                          VALUES (@id, @tenant, @year, 1)
                          ON CONFLICT (tenant_id, year)
                          DO UPDATE SET last_value = billing.invoice_sequence.last_value + 1
                          RETURNING last_value
                          """;

        AddParameter(cmd, "id", LyoGuid.CreateV7());
        AddParameter(cmd, "tenant", seqTenant);
        AddParameter(cmd, "year", year);
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Invoice sequence did not return a value.");
        suppress.Complete();
        return Convert.ToInt32(result);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private async Task SnapshotBillToAsync(BillingDbContext context, InvoiceEntity invoice, BillingClientRecord client, Guid? tenantId, CancellationToken ct)
    {
        invoice.BillToName = client.LegalName ?? client.DisplayName;
        invoice.Notes ??= client.DefaultInvoiceNotes;
        invoice.BillToContactName = client.ContactName;
        invoice.BillToLine1 = client.Line1;
        invoice.BillToLine2 = client.Line2;
        invoice.BillToCity = client.City;
        invoice.BillToRegion = client.Region;
        invoice.BillToPostalCode = client.PostalCode;
        invoice.BillToCountry = client.Country;
        var relationTenant = tenantId ?? Guid.Empty;
        var contact = await context.Contacts.AsNoTracking()
            .WhereActive()
            .WhereTenant(relationTenant)
            .Where(c => c.ClientId == invoice.ClientId && c.IsBillingContact)
            .OrderByDescending(c => c.IsPrimary)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (contact != null) {
            if (!string.IsNullOrWhiteSpace(contact.Title))
                invoice.BillToContactName = contact.Title;

            if (string.Equals(contact.SubjectEntityType, BillingEntityTypes.BillingPerson, StringComparison.Ordinal) && Guid.TryParse(contact.SubjectEntityId, out var personId)) {
                var person = await context.People.AsNoTracking().FirstOrDefaultAsync(p => p.Id == personId, ct).ConfigureAwait(false);
                if (person != null)
                    invoice.BillToContactName = person.DisplayName;
            }
        }

        var address = await context.Addresses.AsNoTracking()
            .WhereActive()
            .WhereTenant(relationTenant)
            .Where(c => c.ClientId == invoice.ClientId && c.Usage == (int)BillingAddressUsage.Billing)
            .OrderByDescending(c => c.IsPrimary)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (address != null && string.Equals(address.SubjectEntityType, BillingEntityTypes.BillingAddress, StringComparison.Ordinal) &&
            Guid.TryParse(address.SubjectEntityId, out var addressId)) {
            var payload = await context.AddressPayloads.AsNoTracking().FirstOrDefaultAsync(a => a.Id == addressId, ct).ConfigureAwait(false);
            if (payload != null) {
                invoice.BillToLine1 = payload.Line1;
                invoice.BillToLine2 = payload.Line2;
                invoice.BillToCity = payload.City;
                invoice.BillToRegion = payload.Region;
                invoice.BillToPostalCode = payload.PostalCode;
                invoice.BillToCountry = payload.Country;
            }
        }

        if (!string.IsNullOrWhiteSpace(client.TaxId)) {
            invoice.TaxIdLast4 = BillingTaxId.Last4(client.TaxId);
            EncryptSecret(client.TaxId, v => invoice.TaxId = v, encrypted => invoice.EncryptedTaxId = encrypted);
        }
    }

    private static void ApplyLineAmounts(IEnumerable<InvoiceLineEntity> lines)
    {
        var running = 0m;
        foreach (var line in lines.OrderBy(l => l.LineNumber)) {
            var pricing = (InvoiceLinePricing)line.Pricing;
            InvoiceLinePricingMath.Validate(pricing, line.Quantity, line.UnitAmount);
            line.Quantity = InvoiceLinePricingMath.NormalizedQuantity(pricing, line.Quantity);
            line.Amount = InvoiceLinePricingMath.Compute(pricing, line.Quantity, line.UnitAmount, running);
            running += line.Amount;
        }
    }

    private void EncryptSecret(string? plaintext, Action<string?> setPlain, Action<byte[]?> setEncrypted)
    {
        if (string.IsNullOrWhiteSpace(plaintext)) {
            setPlain(null);
            return;
        }

        if (_encryption is not { IsEncryptionEnabled: true })
            throw new BillingEncryptionRequiredException("Saving a tax id or remittance secret requires IBillingFieldEncryptionService with a registered IEncryptionService.");

        var value = plaintext;
        byte[]? encrypted = null;
        _encryption.Encrypt(ref value, ref encrypted);
        setEncrypted(encrypted);
        setPlain(null);
    }

    private async Task<LedgerAccountRecord> RequireAccountAsync(string code, Guid? tenantId, CancellationToken ct)
        => await _ledger.GetAccountByCodeAsync(code, tenantId, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Ledger account '{code}' was not found. Call EnsureChartOfAccountsAsync first.");

    private static string TruncateTitle(string title) => title.Length <= 200 ? title : title[..200].TrimEnd();

    private static void RecalcTotals(InvoiceEntity invoice, bool taxExempt)
    {
        invoice.Subtotal = invoice.Lines.Sum(l => l.Amount);
        invoice.TaxAmount = taxExempt || invoice.TaxRate == 0 ? 0m : decimal.Round(invoice.Subtotal * invoice.TaxRate, 4, MidpointRounding.AwayFromZero);
        invoice.Total = invoice.Subtotal + invoice.TaxAmount;
    }

    private async Task<InvoiceRecord?> LoadInvoiceAsync(BillingDbContext context, Guid id, Guid? tenantId, CancellationToken ct)
    {
        var entity = await context.Invoices.AsNoTracking().Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct).ConfigureAwait(false);
        return entity == null ? null : ToInvoice(entity);
    }

    private async Task<BillingClientRecord> RequireClientAsync(BillingDbContext context, Guid clientId, Guid? tenantId, CancellationToken ct)
    {
        var entity = await context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Client '{clientId}' was not found.");

        return ToClient(entity);
    }

    private BillingClientRecord ToClient(BillingClientEntity e)
        => new() {
            Id = e.Id,
            TenantId = e.TenantId,
            DisplayName = e.DisplayName,
            LegalName = e.LegalName,
            Status = (BillingClientStatus)e.Status,
            TaxExempt = e.TaxExempt,
            TaxId = _encryption?.Decrypt(e.EncryptedTaxId),
            TaxIdLast4 = e.TaxIdLast4,
            DefaultCurrency = e.DefaultCurrency,
            PaymentTermsDays = e.PaymentTermsDays,
            InvoicePrefix = e.InvoicePrefix,
            DefaultInvoiceNotes = e.DefaultInvoiceNotes,
            ContactName = e.ContactName,
            Line1 = e.Line1,
            Line2 = e.Line2,
            City = e.City,
            Region = e.Region,
            PostalCode = e.PostalCode,
            Country = e.Country,
            CreatedAt = e.CreatedAt
        };

    private InvoiceRecord ToInvoice(InvoiceEntity e)
        => new() {
            Id = e.Id,
            TenantId = e.TenantId,
            ClientId = e.ClientId,
            Number = e.Number,
            Status = (InvoiceStatus)e.Status,
            Currency = e.Currency,
            TaxRate = e.TaxRate,
            Subtotal = e.Subtotal,
            TaxAmount = e.TaxAmount,
            Total = e.Total,
            AmountPaid = e.AmountPaid,
            IssuedAt = e.IssuedAt,
            DueAt = e.DueAt,
            IssueJournalId = e.IssueJournalId,
            BillToName = e.BillToName,
            BillToContactName = e.BillToContactName,
            BillToLine1 = e.BillToLine1,
            BillToLine2 = e.BillToLine2,
            BillToCity = e.BillToCity,
            BillToRegion = e.BillToRegion,
            BillToPostalCode = e.BillToPostalCode,
            BillToCountry = e.BillToCountry,
            TaxId = _encryption?.Decrypt(e.EncryptedTaxId),
            TaxIdLast4 = e.TaxIdLast4,
            Notes = e.Notes,
            CreatedAt = e.CreatedAt,
            Lines = e.Lines.OrderBy(l => l.LineNumber).Select(ToLine).ToList()
        };

    private static InvoiceLineRecord ToLine(InvoiceLineEntity e)
        => new() {
            Id = e.Id,
            InvoiceId = e.InvoiceId,
            ChargeId = e.ChargeId,
            LineNumber = e.LineNumber,
            Title = e.Title,
            Description = e.Description,
            Quantity = e.Quantity,
            UnitAmount = e.UnitAmount,
            Amount = e.Amount,
            Pricing = (InvoiceLinePricing)e.Pricing
        };

    private static ChargeRecord ToCharge(ChargeEntity e)
        => new() {
            Id = e.Id,
            TenantId = e.TenantId,
            ClientId = e.ClientId,
            InvoiceId = e.InvoiceId,
            Kind = (ChargeKind)e.Kind,
            Description = e.Description,
            Quantity = e.Quantity,
            UnitAmount = e.UnitAmount,
            Currency = e.Currency,
            OccurredAt = e.OccurredAt,
            CreatedAt = e.CreatedAt,
            CreatedBy = e.CreatedByEntityType is { Length: > 0 } t && e.CreatedByEntityId is { Length: > 0 } i ? EntityRef.ForKey(t, i) : null
        };

    private static PaymentRecord ToPayment(PaymentEntity e)
        => new() {
            Id = e.Id,
            TenantId = e.TenantId,
            InvoiceId = e.InvoiceId,
            Amount = e.Amount,
            Currency = e.Currency,
            Method = (PaymentMethod)e.Method,
            Reference = e.Reference,
            JournalId = e.JournalId,
            ReceivedAt = e.ReceivedAt
        };

    private async Task<TRecord> SavePayloadAsync<TRecord, TEntity>(
        TRecord record,
        Guid? tenantId,
        CancellationToken ct,
        Func<BillingDbContext, DbSet<TEntity>> set,
        Func<TRecord, Guid> getId,
        Action<TRecord, Guid> setId,
        Action<TRecord> validate,
        Action<TRecord, TEntity> copy,
        Func<TEntity, TRecord> toRecord)
        where TEntity : class, new()
    {
        ArgumentHelpers.ThrowIfNull(record);
        validate(record);
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var dbSet = set(context);
        var id = getId(record);
        TEntity entity;
        if (id != Guid.Empty) {
            var existing = await dbSet.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id && EF.Property<Guid?>(e, "TenantId") == resolvedTenant, ct).ConfigureAwait(false);
            entity = existing ?? new TEntity();
            if (existing == null) {
                copy(record, entity);
                SetPayloadIdAndTenant(entity, id, resolvedTenant);
                dbSet.Add(entity);
            }
            else
                copy(record, entity);
        }
        else {
            entity = new();
            copy(record, entity);
            var newId = LyoGuid.CreateV7();
            SetPayloadIdAndTenant(entity, newId, resolvedTenant);
            setId(record, newId);
            dbSet.Add(entity);
        }

        StampCreated(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return toRecord(entity);
    }

    private async Task<TRecord?> GetPayloadAsync<TRecord, TEntity>(
        Guid id,
        Guid? tenantId,
        CancellationToken ct,
        Func<BillingDbContext, DbSet<TEntity>> set,
        Func<TEntity, TRecord> toRecord)
        where TRecord : class where TEntity : class
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await set(context)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id && EF.Property<Guid?>(e, "TenantId") == resolvedTenant, ct)
            .ConfigureAwait(false);

        return entity == null ? null : toRecord(entity);
    }

    private async Task<TRecord> AttachUsageLinkAsync<TEntity, TRecord>(
        Guid clientId,
        EntityRef payload,
        int usage,
        bool isPrimary,
        EntityRef? contact,
        Guid? tenantId,
        CancellationToken ct,
        Func<BillingDbContext, DbSet<TEntity>> set,
        Action<TEntity, int, bool, EntityRef?> apply,
        Func<TEntity, TRecord> toRecord)
        where TEntity : EntityRelationEntityBase, new()
    {
        ArgumentHelpers.ThrowIfNull(payload);
        var resolvedTenant = ResolveTenant(tenantId);
        var relationTenant = resolvedTenant ?? Guid.Empty;
        var forId = EntityRefPersistedGuid.PersistedEntityId(payload);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await EnsureClientAsync(context, clientId, resolvedTenant, ct).ConfigureAwait(false);
        var dbSet = set(context);
        var existing = await dbSet.WhereActive()
            .WhereTenant(relationTenant)
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "ClientId") == clientId && e.SubjectEntityType == payload.EntityType && e.SubjectEntityId == forId, ct)
            .ConfigureAwait(false);

        if (existing != null) {
            apply(existing, usage, isPrimary, contact);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            return toRecord(existing);
        }

        var actor = EntityRef.ForGuid(EntityRefWellKnown.SystemActorType, EntityRefWellKnown.SystemActorId);
        var entity = new TEntity {
            Id = LyoGuid.CreateV7(),
            SubjectEntityType = payload.EntityType,
            SubjectEntityId = forId,
            ActorEntityType = actor.EntityType,
            ActorEntityId = EntityRefPersistedGuid.PersistedEntityId(actor),
            TenantId = relationTenant,
            CreatedAt = DateTime.UtcNow,
            Visibility = EntityRefVisibility.Private
        };

        typeof(TEntity).GetProperty("ClientId")!.SetValue(entity, clientId);
        apply(entity, usage, isPrimary, contact);
        dbSet.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return toRecord(entity);
    }

    private async Task SoftDeleteAsync<TEntity>(Guid linkId, Guid? tenantId, CancellationToken ct, Func<BillingDbContext, DbSet<TEntity>> set)
        where TEntity : EntityRelationEntityBase
    {
        var relationTenant = ResolveTenant(tenantId) ?? Guid.Empty;
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await set(context).WhereActive().WhereTenant(relationTenant).FirstOrDefaultAsync(e => e.Id == linkId, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        entity.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static async Task EnsureClientAsync(BillingDbContext context, Guid clientId, Guid? tenantId, CancellationToken ct)
    {
        var exists = await context.Clients.AnyAsync(c => c.Id == clientId && c.TenantId == tenantId, ct).ConfigureAwait(false);
        if (!exists)
            throw new InvalidOperationException($"Client '{clientId}' was not found.");
    }

    private static void SetPayloadIdAndTenant(object entity, Guid id, Guid? tenantId)
    {
        entity.GetType().GetProperty("Id")!.SetValue(entity, id);
        entity.GetType().GetProperty("TenantId")!.SetValue(entity, tenantId);
    }

    private static void StampCreated(object entity)
    {
        var created = entity.GetType().GetProperty("CreatedTimestamp");
        if (created != null && created.GetValue(entity) is DateTime dt && dt == default)
            created.SetValue(entity, DateTime.UtcNow);
    }

    private static void ApplyUsage(BillingPhoneEntity e, int usage, bool isPrimary, EntityRef? contact)
    {
        e.Usage = usage;
        e.IsPrimary = isPrimary;
        SetContactRef(
            e, contact, (row, t, i) => {
                row.ContactEntityType = t;
                row.ContactEntityId = i;
            });
    }

    private static void ApplyUsage(BillingEmailEntity e, int usage, bool isPrimary, EntityRef? contact)
    {
        e.Usage = usage;
        e.IsPrimary = isPrimary;
        SetContactRef(
            e, contact, (row, t, i) => {
                row.ContactEntityType = t;
                row.ContactEntityId = i;
            });
    }

    private static void ApplyUsage(BillingAddressEntity e, int usage, bool isPrimary, EntityRef? contact)
    {
        e.Usage = usage;
        e.IsPrimary = isPrimary;
        SetContactRef(
            e, contact, (row, t, i) => {
                row.ContactEntityType = t;
                row.ContactEntityId = i;
            });
    }

    private static void SetContactRef<T>(T entity, EntityRef? contact, Action<T, string?, string?> apply)
    {
        if (contact == null) {
            apply(entity, null, null);
            return;
        }

        apply(entity, contact.Value.EntityType, EntityRefPersistedGuid.PersistedEntityId(contact.Value));
    }

    private static BillingContactRecord ToContact(BillingContactEntity e)
        => new() {
            Id = e.Id,
            ClientId = e.ClientId,
            Contact = EntityRef.ForKey(e.SubjectEntityType ?? BillingEntityTypes.BillingPerson, e.SubjectEntityId ?? string.Empty),
            Role = (BillingContactRole)e.Role,
            Title = e.Title,
            IsPrimary = e.IsPrimary,
            IsBillingContact = e.IsBillingContact
        };

    private static BillingPhoneLinkRecord ToPhoneLink(BillingPhoneEntity e)
        => new() {
            Id = e.Id,
            ClientId = e.ClientId,
            Phone = EntityRef.ForKey(e.SubjectEntityType ?? BillingEntityTypes.BillingPhoneNumber, e.SubjectEntityId ?? string.Empty),
            Usage = (BillingPhoneUsage)e.Usage,
            IsPrimary = e.IsPrimary,
            Contact = e.ContactEntityType is { Length: > 0 } t && e.ContactEntityId is { Length: > 0 } i ? EntityRef.ForKey(t, i) : null
        };

    private static BillingEmailLinkRecord ToEmailLink(BillingEmailEntity e)
        => new() {
            Id = e.Id,
            ClientId = e.ClientId,
            Email = EntityRef.ForKey(e.SubjectEntityType ?? BillingEntityTypes.BillingEmailAddress, e.SubjectEntityId ?? string.Empty),
            Usage = (BillingEmailUsage)e.Usage,
            IsPrimary = e.IsPrimary,
            Contact = e.ContactEntityType is { Length: > 0 } t && e.ContactEntityId is { Length: > 0 } i ? EntityRef.ForKey(t, i) : null
        };

    private static BillingAddressLinkRecord ToAddressLink(BillingAddressEntity e)
        => new() {
            Id = e.Id,
            ClientId = e.ClientId,
            Address = EntityRef.ForKey(e.SubjectEntityType ?? BillingEntityTypes.BillingAddress, e.SubjectEntityId ?? string.Empty),
            Usage = (BillingAddressUsage)e.Usage,
            IsPrimary = e.IsPrimary,
            Contact = e.ContactEntityType is { Length: > 0 } t && e.ContactEntityId is { Length: > 0 } i ? EntityRef.ForKey(t, i) : null
        };
}