using Bogus;
using Lyo.Billing.Postgres;
using Lyo.Billing.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Billing.Seed;

/// <summary>EF seed of billing clients with draft and issued invoices, charges, payments, a biller profile, and contact payloads. No tax id and no ledger post. Schema <c>billing</c>.</summary>
public sealed class BillingEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Billing";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresBillingOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<BillingDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new BillingDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var personClient = new Dictionary<Guid, Guid>();
        var phoneClient = new Dictionary<Guid, Guid>();
        var emailClient = new Dictionary<Guid, Guid>();
        var addressClient = new Dictionary<Guid, Guid>();
        var clients = graph.Entity(
            options.Count, _ => new BillingClientEntity {
                Id = LyoGuid.CreateV7(),
                DisplayName = faker.Company.CompanyName(),
                Status = (int)BillingClientStatus.Active,
                DefaultCurrency = "USD",
                CreatedAt = now
            });

        var invoices = clients.ForEach(
            _ => 2, (client, n) => {
                var amount = faker.Random.Decimal(10, 500);
                var issued = n == 1;
                return new InvoiceEntity {
                    Id = LyoGuid.CreateV7(),
                    ClientId = client.Id,
                    Status = (int)(issued ? InvoiceStatus.Issued : InvoiceStatus.Draft),
                    Number = issued ? $"INV-{client.Id:N}" : null,
                    Currency = "USD",
                    Subtotal = amount,
                    Total = amount,
                    AmountPaid = issued ? amount : 0,
                    IssuedAt = issued ? now : null,
                    BillToName = client.DisplayName,
                    CreatedAt = now
                };
            });

        invoices.ForEach(
            _ => 1, (invoice, _) => new InvoiceLineEntity {
                Id = LyoGuid.CreateV7(),
                InvoiceId = invoice.Id,
                LineNumber = 1,
                Title = faker.Commerce.ProductName(),
                Description = invoice.Status == (int)InvoiceStatus.Issued ? "Issued time" : "Draft time",
                Quantity = 1,
                UnitAmount = invoice.Subtotal,
                Amount = invoice.Subtotal
            });

        invoices.ForEach(
            invoice => invoice.Status == (int)InvoiceStatus.Issued ? 1 : 0,
            (invoice, _) => new ChargeEntity {
                Id = LyoGuid.CreateV7(),
                ClientId = invoice.ClientId,
                InvoiceId = invoice.Id,
                Kind = (int)ChargeKind.Time,
                Description = faker.Commerce.ProductName(),
                Quantity = 1,
                UnitAmount = invoice.Subtotal,
                Currency = "USD",
                OccurredAt = now,
                CreatedAt = now
            });

        invoices.ForEach(
            invoice => invoice.Status == (int)InvoiceStatus.Issued ? 1 : 0,
            (invoice, _) => new PaymentEntity {
                Id = LyoGuid.CreateV7(),
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                Currency = "USD",
                Method = (int)PaymentMethod.Check,
                Reference = invoice.Number ?? "check",
                ReceivedAt = now
            });

        clients.ForEach(
                _ => 1, (client, _) => {
                    var person = new BillingPersonEntity { Id = LyoGuid.CreateV7(), DisplayName = client.DisplayName, CreatedTimestamp = now };
                    personClient[person.Id] = client.Id;
                    return person;
                })
            .ForEach(
                _ => 1, (person, _) => new BillingContactEntity {
                    Id = LyoGuid.CreateV7(),
                    ClientId = personClient[person.Id],
                    SubjectEntityType = BillingEntityTypes.BillingPerson,
                    SubjectEntityId = person.Id.ToString(),
                    ActorEntityType = "System",
                    ActorEntityId = "00000000-0000-4000-8000-000000000002",
                    TenantId = Guid.Empty,
                    Role = (int)BillingContactRole.Billing,
                    IsPrimary = true,
                    IsBillingContact = true,
                    CreatedAt = now
                });

        clients.ForEach(
                _ => 1, (client, _) => {
                    var phone = new BillingPhoneNumberEntity {
                        Id = LyoGuid.CreateV7(),
                        Number = faker.Phone.PhoneNumber("###-###-####"),
                        Country = "US",
                        CreatedTimestamp = now
                    };

                    phoneClient[phone.Id] = client.Id;
                    return phone;
                })
            .ForEach(
                _ => 1, (phone, _) => new BillingPhoneEntity {
                    Id = LyoGuid.CreateV7(),
                    ClientId = phoneClient[phone.Id],
                    SubjectEntityType = BillingEntityTypes.BillingPhoneNumber,
                    SubjectEntityId = phone.Id.ToString(),
                    ActorEntityType = "System",
                    ActorEntityId = "00000000-0000-4000-8000-000000000002",
                    TenantId = Guid.Empty,
                    Usage = (int)BillingPhoneUsage.Work,
                    IsPrimary = true,
                    CreatedAt = now
                });

        clients.ForEach(
                _ => 1, (client, _) => {
                    var email = new BillingEmailAddressEntity { Id = LyoGuid.CreateV7(), Email = faker.Internet.Email(), CreatedTimestamp = now };
                    emailClient[email.Id] = client.Id;
                    return email;
                })
            .ForEach(
                _ => 1, (email, _) => new BillingEmailEntity {
                    Id = LyoGuid.CreateV7(),
                    ClientId = emailClient[email.Id],
                    SubjectEntityType = BillingEntityTypes.BillingEmailAddress,
                    SubjectEntityId = email.Id.ToString(),
                    ActorEntityType = "System",
                    ActorEntityId = "00000000-0000-4000-8000-000000000002",
                    TenantId = Guid.Empty,
                    Usage = (int)BillingEmailUsage.Billing,
                    IsPrimary = true,
                    CreatedAt = now
                });

        clients.ForEach(
                _ => 1, (client, _) => {
                    var address = new BillingAddressPayloadEntity {
                        Id = LyoGuid.CreateV7(),
                        Line1 = faker.Address.StreetAddress(),
                        City = faker.Address.City(),
                        Region = faker.Address.StateAbbr(),
                        PostalCode = faker.Address.ZipCode(),
                        Country = "US",
                        CreatedTimestamp = now
                    };

                    addressClient[address.Id] = client.Id;
                    return address;
                })
            .ForEach(
                _ => 1, (address, _) => new BillingAddressEntity {
                    Id = LyoGuid.CreateV7(),
                    ClientId = addressClient[address.Id],
                    SubjectEntityType = BillingEntityTypes.BillingAddress,
                    SubjectEntityId = address.Id.ToString(),
                    ActorEntityType = "System",
                    ActorEntityId = "00000000-0000-4000-8000-000000000002",
                    TenantId = Guid.Empty,
                    Usage = (int)BillingAddressUsage.Billing,
                    IsPrimary = true,
                    CreatedAt = now
                });

        graph.Entity(
            1, _ => new BillerProfileEntity {
                Id = LyoGuid.CreateV7(),
                DisplayName = faker.Company.CompanyName(),
                LegalName = faker.Company.CompanyName(),
                Remittance = "Pay to the address on the invoice"
            });

        graph.Entity(1, _ => new InvoiceSequenceEntity { Id = LyoGuid.CreateV7(), Year = now.Year, LastValue = options.Count });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<BillingDbContext> ef)
            throw new SeedException("Billing OnClear requires EfSeedTransport<BillingDbContext>.");

        var db = ef.Context;
        await db.Payments.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.InvoiceLines.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Charges.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Invoices.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Contacts.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Phones.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Emails.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Addresses.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Clients.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.People.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.PhoneNumbers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EmailAddresses.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.AddressPayloads.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.InvoiceSequences.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.BillerProfiles.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}