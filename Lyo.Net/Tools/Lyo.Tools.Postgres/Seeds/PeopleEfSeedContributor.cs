using Bogus;
using Lyo.Common.Metadata.Records;
using Lyo.People.Models;
using Lyo.People.Postgres.Database;
using Lyo.Seed;
using Microsoft.Extensions.Logging;

namespace Lyo.Tools.Postgres.Seeds;

/// <summary>EF-direct seed of the people schema (persons, contacts, junctions, relationships).</summary>
public sealed class PeopleEfSeedContributor(ILogger<PeopleEfSeedContributor> logger) : SeedContributor
{
    private static readonly string[] Nationalities = ["US", "GB", "CA", "AU", "DE", "FR", "JP", "MX", "BR", "IN"];
    private static readonly string[] Languages = ["en", "es", "fr", "de", "ja", "zh", "pt", "ko", "ar", "hi"];
    private static readonly string[] PhoneTypes = ["Mobile", "Home", "Work", "Fax", "Other"];
    private static readonly string[] EmailTypes = ["Personal", "Work", "Other"];
    private static readonly string[] AddressTypes = ["Home", "Work", "Billing", "Mailing", "Other"];
    private static readonly SocialPlatformInfo[] SeedSocialPlatforms = SocialPlatformInfo.All.Where(p => p != SocialPlatformInfo.Other).ToArray();
    private static readonly string[] EmployTypes = ["FullTime", "PartTime", "Contract", "Freelance", "Internship"];
    private static readonly string[] RelationTypes = ["Spouse", "Parent", "Child", "Sibling", "Partner", "Friend", "Colleague"];

    /// <inheritdoc />
    public override string Name => "People";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var people = graph.Entity(options.Count, _ => {
            var person = BuildPerson(faker);
            person.SourceEntityType = PeopleSourceTypes.Seed;
            person.SourceEntityId = person.Id.ToString();
            person.ImportedAt = DateTime.UtcNow;
            return person;
        });
        people.ForEach(_ => faker.Random.Int(0, 2), (person, _) => BuildEmployment(faker, person.Id));
        people.ForEach(_ => faker.Random.Int(0, 2), (person, _) => BuildSocialProfile(faker, person.Id));
        graph.After(async (session, ct) => await SeedContactsAndRelationshipsAsync(session, faker, ct).ConfigureAwait(false));
    }

    private async Task SeedContactsAndRelationshipsAsync(SeedSession session, Faker faker, CancellationToken ct)
    {
        var persons = session.Generated<PersonEntity>();
        logger.LogInformation("Seeding contacts for {Count} persons...", persons.Count);
        var phones = new List<(Guid PersonId, PhoneNumberEntity Phone)>();
        var emails = new List<(Guid PersonId, EmailAddressEntity Email)>();
        var addrs = new List<(Guid PersonId, AddressEntity Address)>();
        foreach (var person in persons) {
            var phoneCount = faker.Random.Int(1, 2);
            for (var i = 0; i < phoneCount; i++) {
                var phone = BuildPhone(faker);
                phones.Add((person.Id, phone));
            }

            var emailCount = faker.Random.Int(1, 2);
            for (var i = 0; i < emailCount; i++) {
                var email = BuildEmail(faker, person.FirstName, person.LastName);
                emails.Add((person.Id, email));
            }

            addrs.Add((person.Id, BuildAddress(faker)));
        }

        await session.PersistAsync(phones.Select(p => p.Phone).ToArray(), ct).ConfigureAwait(false);
        await session.PersistAsync(emails.Select(e => e.Email).ToArray(), ct).ConfigureAwait(false);
        await session.PersistAsync(addrs.Select(a => a.Address).ToArray(), ct).ConfigureAwait(false);

        var contactPhones = new List<ContactPhoneNumberEntity>();
        var isPrimaryPhone = new HashSet<Guid>();
        foreach (var (personId, phone) in phones) {
            contactPhones.Add(
                new() {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    PhoneNumberId = phone.Id,
                    Type = faker.PickRandom(PhoneTypes),
                    IsPrimary = isPrimaryPhone.Add(personId),
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        var contactEmails = new List<ContactEmailAddressEntity>();
        var isPrimaryEmail = new HashSet<Guid>();
        foreach (var (personId, email) in emails) {
            contactEmails.Add(
                new() {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    EmailAddressId = email.Id,
                    Type = faker.PickRandom(EmailTypes),
                    IsPrimary = isPrimaryEmail.Add(personId),
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        var contactAddrs = new List<ContactAddressEntity>();
        var isPrimaryAddr = new HashSet<Guid>();
        foreach (var (personId, addr) in addrs) {
            contactAddrs.Add(
                new() {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    AddressId = addr.Id,
                    Type = faker.PickRandom(AddressTypes),
                    IsPrimary = isPrimaryAddr.Add(personId),
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        await session.PersistAsync(contactPhones, ct).ConfigureAwait(false);
        await session.PersistAsync(contactEmails, ct).ConfigureAwait(false);
        await session.PersistAsync(contactAddrs, ct).ConfigureAwait(false);

        var relationships = BuildRelationships(faker, persons.ToList());
        if (relationships.Count > 0)
            await session.PersistAsync(relationships, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Seeded {PersonCount} persons, {PhoneCount} phones, {EmailCount} emails, {AddrCount} addresses, {RelCount} relationships.",
            persons.Count, phones.Count, emails.Count, addrs.Count, relationships.Count);
    }

    private static PersonEntity BuildPerson(Faker faker)
        => new() {
            Id = Guid.NewGuid(),
            FirstName = faker.Name.FirstName(),
            LastName = faker.Name.LastName(),
            MiddleName = faker.Random.Bool(0.4f) ? faker.Name.FirstName() : null,
            PreferredName = faker.Random.Bool(0.2f) ? faker.Name.FirstName() : null,
            DateOfBirth = faker.Random.Bool(0.8f) ? DateOnly.FromDateTime(faker.Date.Between(new(1950, 1, 1), new(2005, 1, 1))) : null,
            Sex = faker.Random.Bool(0.7f) ? faker.PickRandom("M", "F") : null,
            Nationality = faker.Random.Bool(0.7f) ? faker.PickRandom(Nationalities) : null,
            PreferredLanguageBcp47 = faker.Random.Bool(0.6f) ? faker.PickRandom(Languages) : null,
            CurrentJobTitle = faker.Random.Bool(0.6f) ? faker.Name.JobTitle() : null,
            CurrentCompany = faker.Random.Bool(0.5f) ? faker.Company.CompanyName() : null,
            IsActive = faker.Random.Bool(0.9f),
            CreatedTimestamp = faker.Date.Past(3).ToUniversalTime()
        };

    private static PhoneNumberEntity BuildPhone(Faker f)
        => new() {
            Id = Guid.NewGuid(),
            Number = f.Phone.PhoneNumber("##########"),
            CountryCode = "1",
            CountryCodeString = "+1",
            TechnologyType = f.PickRandom("Mobile", "Landline"),
            Label = f.PickRandom("Personal", "Work"),
            CreatedTimestamp = DateTime.UtcNow
        };

    private static EmailAddressEntity BuildEmail(Faker f, string firstName, string lastName)
        => new() {
            Id = Guid.NewGuid(),
            Email = f.Internet.Email(firstName, lastName),
            Label = f.PickRandom("Personal", "Work"),
            CreatedTimestamp = DateTime.UtcNow
        };

    private static AddressEntity BuildAddress(Faker f)
        => new() {
            Id = Guid.NewGuid(),
            HouseNumber = f.Address.BuildingNumber(),
            StreetName = f.Address.StreetName(),
            City = f.Address.City(),
            State = f.Address.StateAbbr(),
            Zipcode = f.Address.ZipCode("#####"),
            CountryCode = "US",
            FullAddress = f.Address.FullAddress(),
            CreatedTimestamp = DateTime.UtcNow
        };

    private static SocialMediaProfileEntity BuildSocialProfile(Faker f, Guid personId)
        => new() {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Platform = f.PickRandom(SeedSocialPlatforms).Slug,
            Username = f.Internet.UserName(),
            ProfileUrl = f.Random.Bool(0.7f) ? f.Internet.Url() : null,
            AddedAt = f.Date.Past(2).ToUniversalTime(),
            CreatedTimestamp = DateTime.UtcNow
        };

    private static EmploymentEntity BuildEmployment(Faker f, Guid personId)
    {
        var start = DateOnly.FromDateTime(f.Date.Past(10));
        var isCurrentJob = f.Random.Bool(0.4f);
        return new() {
            Id = Guid.NewGuid(),
            PersonId = personId,
            CompanyName = f.Company.CompanyName(),
            JobTitle = f.Name.JobTitle(),
            Department = f.Random.Bool(0.5f) ? f.Commerce.Department() : null,
            StartDate = start,
            EndDate = isCurrentJob ? null : DateOnly.FromDateTime(f.Date.Between(start.ToDateTime(TimeOnly.MinValue), DateTime.Now)),
            Type = f.PickRandom(EmployTypes),
            CreatedTimestamp = DateTime.UtcNow
        };
    }

    private static List<PersonRelationshipEntity> BuildRelationships(Faker faker, List<PersonEntity> persons)
    {
        if (persons.Count < 2)
            return [];

        var relationships = new List<PersonRelationshipEntity>();
        var pairCount = Math.Max(1, persons.Count / 5);
        var shuffled = faker.Random.Shuffle(persons).ToList();
        for (var i = 0; i < pairCount && i * 2 + 1 < shuffled.Count; i++) {
            var a = shuffled[i * 2];
            var b = shuffled[i * 2 + 1];
            relationships.Add(
                new() {
                    Id = Guid.NewGuid(),
                    PersonId = a.Id,
                    RelatedPersonId = b.Id,
                    Type = faker.PickRandom(RelationTypes),
                    IsActive = true,
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        return relationships;
    }
}
