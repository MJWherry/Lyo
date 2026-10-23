using Lyo.Billing.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Billing.Seed.Tests;

public sealed class BillingEfSeedContributorTests
{
    [Fact]
    public async Task BillingEfSeedContributor_Count_PersistsClientsWithDraftInvoices()
    {
        var transport = await SeedAsync(new BillingEfSeedContributor(), 3);
        var clients = transport.Items.OfType<BillingClientEntity>().ToList();
        var invoices = transport.Items.OfType<InvoiceEntity>().ToList();
        var lines = transport.Items.OfType<InvoiceLineEntity>().ToList();
        var charges = transport.Items.OfType<ChargeEntity>().ToList();
        var payments = transport.Items.OfType<PaymentEntity>().ToList();
        var people = transport.Items.OfType<BillingPersonEntity>().ToList();
        Assert.Equal(3, clients.Count);
        Assert.Equal(6, invoices.Count);
        Assert.Equal(6, lines.Count);
        Assert.Single(transport.Items.OfType<BillerProfileEntity>());
        Assert.Single(transport.Items.OfType<InvoiceSequenceEntity>());
        Assert.Equal(3, people.Count);
        Assert.Equal(3, transport.Items.OfType<BillingPhoneNumberEntity>().Count());
        Assert.Equal(3, transport.Items.OfType<BillingEmailAddressEntity>().Count());
        Assert.Equal(3, transport.Items.OfType<BillingAddressPayloadEntity>().Count());
        Assert.Equal(3, transport.Items.OfType<BillingContactEntity>().Count());
        Assert.Equal(3, transport.Items.OfType<BillingPhoneEntity>().Count());
        Assert.Equal(3, transport.Items.OfType<BillingEmailEntity>().Count());
        Assert.Equal(3, transport.Items.OfType<BillingAddressEntity>().Count());
        Assert.Equal(3, charges.Count);
        Assert.Equal(3, payments.Count);
        Assert.All(payments, payment => Assert.Null(payment.JournalId));
        Assert.All(
            clients, client => {
                Assert.Null(client.TaxId);
                Assert.Null(client.EncryptedTaxId);
                Assert.Contains(invoices, invoice => invoice.ClientId == client.Id && invoice.Status == (int)InvoiceStatus.Draft);
                var issued = Assert.Single(invoices, invoice => invoice.ClientId == client.Id && invoice.Status == (int)InvoiceStatus.Issued);
                Assert.Contains(client.Id.ToString("N"), issued.Number, StringComparison.Ordinal);
                Assert.Contains(lines, line => line.InvoiceId == issued.Id);
                Assert.Contains(charges, charge => charge.InvoiceId == issued.Id && charge.ClientId == client.Id);
                Assert.Contains(payments, payment => payment.InvoiceId == issued.Id);
                Assert.Contains(transport.Items.OfType<BillingContactEntity>(), link => link.ClientId == client.Id);
            });

        Assert.All(invoices, invoice => Assert.Contains(lines, line => line.InvoiceId == invoice.Id));
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor, int count)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = count, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return transport;
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            foreach (var item in items)
                Items.Add(item);

            return Task.CompletedTask;
        }
    }
}