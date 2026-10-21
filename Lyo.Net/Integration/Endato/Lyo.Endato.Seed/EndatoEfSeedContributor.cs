using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Endato.Postgres;
using Lyo.Endato.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Endato.Seed;

/// <summary>EF seed of one person-search query and one contact-enrichment query per count, each with a person, address, email, and phone. Schema <c>endato</c>.</summary>
public sealed class EndatoEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Endato";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresEndatoOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<EndatoDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new EndatoDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTimeOffset.UtcNow;
        var day = DateOnly.FromDateTime(now.UtcDateTime);
        var people = graph
            .Entity(
                options.Count, i => new EndatoPsQueryEntity {
                    Id = LyoGuid.CreateV7(),
                    FirstName = Fit(faker.Name.FirstName(), 25),
                    LastName = PersonLast(faker, i),
                    DateOfBirth = new(1990, 1, 1),
                    RequestId = LyoGuid.CreateV7(),
                    RequestTime = 12,
                    RequestTimestamp = now
                })
            .ForEach(
                _ => 1, (query, _) => new EndatoPsPersonEntity {
                    Id = LyoGuid.CreateV7(),
                    QueryId = query.Id,
                    FirstName = query.FirstName,
                    LastName = query.LastName,
                    DateOfBirth = query.DateOfBirth
                });

        people.ForEach(
            _ => 1, (person, _) => new EndatoPsAddressEntity {
                Id = LyoGuid.CreateV7(),
                EndatoPersonId = person.Id,
                IsDeliverable = true,
                AddressHash = person.Id.ToString("N")[^25..],
                HouseNumber = "100",
                StreetName = Fit(faker.Address.StreetName(), 40),
                City = Fit(faker.Address.City(), 25),
                State = faker.Address.StateAbbr(),
                Zipcode = faker.Address.ZipCode("#####"),
                FullAddress = Fit($"{person.LastName}, {faker.Address.City()}", 100),
                OrderNumber = 1,
                FirstReportedDate = day,
                LastReportedDate = day,
                PublicFirstSeenDate = day,
                TotalFirstSeenDate = day
            });

        people.ForEach(
            _ => 1, (person, _) => new EndatoPsEmailAddressEntity {
                Id = LyoGuid.CreateV7(),
                EndatoPersonId = person.Id,
                Address = $"{person.Id:N}@example.com",
                OrderNumber = 1
            });

        people.ForEach(
            _ => 1, (person, _) => new EndatoPsPhoneNumberEntity {
                Id = LyoGuid.CreateV7(),
                EndatoPersonId = person.Id,
                Number = faker.Phone.PhoneNumber("###-###-####"),
                Type = "mobile",
                IsConnected = true,
                OrderNumber = 1,
                FirstReportedDate = day,
                LastReportedDate = day,
                PublicFirstSeenDate = day,
                TotalFirstSeenDate = day
            });

        var cePeople = graph.Entity(
            options.Count, i => new EndatoCePersonEntity {
                Id = LyoGuid.CreateV7(),
                FirstName = Fit(faker.Name.FirstName(), 25),
                LastName = PersonLast(faker, i),
                DateOfBirth = new DateOnly(1990, 1, 1)
            });

        cePeople.ForEach(
            _ => 1, (person, _) => new EndatoCeQueryEntity {
                Id = LyoGuid.CreateV7(),
                FirstName = person.FirstName,
                LastName = person.LastName,
                AddressLineTwo = "Pittsburgh PA",
                IdentityScore = 80,
                EndatoCePersonId = person.Id,
                RequestId = LyoGuid.CreateV7(),
                RequestTime = now,
                RequestTimestamp = now
            });

        cePeople.ForEach(
            _ => 1, (person, _) => new EndatoCeAddressEntity {
                Id = LyoGuid.CreateV7(),
                EndatoCePersonId = person.Id,
                Street = Fit(faker.Address.StreetAddress(), 75),
                City = Fit(faker.Address.City(), 25),
                State = faker.Address.StateAbbr(),
                Zipcode = faker.Address.ZipCode("#####"),
                FirstReportedDate = day,
                LastReportedDate = day
            });

        cePeople.ForEach(
            _ => 1, (person, _) => new EndatoCeEmailAddressEntity {
                Id = LyoGuid.CreateV7(),
                EndatoCePersonId = person.Id,
                Email = $"{person.Id:N}@example.com",
                IsValidated = true
            });

        cePeople.ForEach(
            _ => 1, (person, _) => new EndatoCePhoneNumberEntity {
                Id = LyoGuid.CreateV7(),
                EndatoCePersonId = person.Id,
                Number = faker.Phone.PhoneNumber("###-###-####"),
                Type = "mobile",
                IsConnected = true,
                FirstReportedDate = day,
                LastReportedDate = day
            });
    }

    private static string Fit(string value, int max) => value.Length <= max ? value : value[..max];

    private static string PersonLast(Faker faker, int index)
    {
        var suffix = index.ToString();
        var room = 25 - suffix.Length;
        var last = faker.Name.LastName();
        return (room <= 0 ? "" : last.Length <= room ? last : last[..room]) + suffix;
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<EndatoDbContext> ef)
            throw new SeedException("Endato OnClear requires EfSeedTransport<EndatoDbContext>.");

        var db = ef.Context;
        await db.EndatoPsPhoneNumbers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoPsEmailAddresses.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoPsAddresses.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoPsPersons.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoPsQueries.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoCePhoneNumbers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoCeEmailAddresses.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoCeAddresses.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoCeQueries.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.EndatoCePersons.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}