using Bogus;
using Lyo.People.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Tools.Postgres.Seeds;

/// <summary>Seeds the people database with randomised fake data using Bogus. Creates a PeopleDbContext directly from the active ConnectionStringProvider.</summary>
public sealed class PeopleDbSeeder
{
    private static readonly string[] Nationalities = ["US", "GB", "CA", "AU", "DE", "FR", "JP", "MX", "BR", "IN"];
    private static readonly string[] Languages = ["en", "es", "fr", "de", "ja", "zh", "pt", "ko", "ar", "hi"];
    private static readonly string[] PhoneTypes = ["Mobile", "Home", "Work", "Fax", "Other"];
    private static readonly string[] EmailTypes = ["Personal", "Work", "Other"];
    private static readonly string[] AddressTypes = ["Home", "Work", "Billing", "Mailing", "Other"];
    private static readonly string[] SocialPlatforms = ["LinkedIn", "Twitter", "Instagram", "Facebook", "GitHub", "TikTok", "YouTube"];
    private static readonly string[] EmployTypes = ["FullTime", "PartTime", "Contract", "Freelance", "Internship"];
    private static readonly string[] RelationTypes = ["Spouse", "Parent", "Child", "Sibling", "Partner", "Friend", "Colleague"];
    private readonly ConnectionStringProvider _connStr;
    private readonly ILogger<PeopleDbSeeder> _logger;

    public PeopleDbSeeder(ConnectionStringProvider connStr, ILogger<PeopleDbSeeder> logger)
    {
        _connStr = connStr;
        _logger = logger;
    }

    /// <summary>Seeds the database with fake person records. Skips if any persons already exist.</summary>
    public async Task SeedAsync(int count = 50, int? seed = null, CancellationToken ct = default)
    {
        await using var db = CreateContext();
        if (await db.Persons.AnyAsync(ct)) {
            _logger.LogInformation("People DB already has data — skipping seed.");
            return;
        }

        var faker = seed.HasValue ? new Faker { Random = new(seed.Value) } : new Faker();
        _logger.LogInformation("Seeding {Count} persons...", count);
        var persons = BuildPersons(faker, count);
        db.Persons.AddRange(persons);
        await db.SaveChangesAsync(ct);
        var phones = new List<(Guid personId, PhoneNumberEntity phone)>();
        var emails = new List<(Guid personId, EmailAddressEntity email)>();
        var addrs = new List<(Guid personId, AddressEntity address)>();
        var socials = new List<SocialMediaProfileEntity>();
        var jobs = new List<EmploymentEntity>();
        foreach (var person in persons) {
            var phoneCount = faker.Random.Int(1, 2);
            for (var i = 0; i < phoneCount; i++) {
                var phone = BuildPhone(faker);
                db.PhoneNumbers.Add(phone);
                phones.Add((person.Id, phone));
            }

            var emailCount = faker.Random.Int(1, 2);
            for (var i = 0; i < emailCount; i++) {
                var email = BuildEmail(faker, person.FirstName, person.LastName);
                db.EmailAddresses.Add(email);
                emails.Add((person.Id, email));
            }

            var addr = BuildAddress(faker);
            db.Addresses.Add(addr);
            addrs.Add((person.Id, addr));
            var socialCount = faker.Random.Int(0, 2);
            for (var i = 0; i < socialCount; i++)
                socials.Add(BuildSocialProfile(faker, person.Id));

            var jobCount = faker.Random.Int(0, 2);
            for (var i = 0; i < jobCount; i++)
                jobs.Add(BuildEmployment(faker, person.Id));
        }

        if (socials.Count > 0)
            db.SocialMediaProfiles.AddRange(socials);

        if (jobs.Count > 0)
            db.Employments.AddRange(jobs);

        await db.SaveChangesAsync(ct);

        // Link contacts (phones, emails, addresses)
        var isPrimaryPhone = new HashSet<Guid>();
        foreach (var (personId, phone) in phones) {
            db.ContactPhoneNumbers.Add(
                new() {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    PhoneNumberId = phone.Id,
                    Type = faker.PickRandom(PhoneTypes),
                    IsPrimary = isPrimaryPhone.Add(personId),
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        var isPrimaryEmail = new HashSet<Guid>();
        foreach (var (personId, email) in emails) {
            db.ContactEmailAddresses.Add(
                new() {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    EmailAddressId = email.Id,
                    Type = faker.PickRandom(EmailTypes),
                    IsPrimary = isPrimaryEmail.Add(personId),
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        var isPrimaryAddr = new HashSet<Guid>();
        foreach (var (personId, addr) in addrs) {
            db.ContactAddresses.Add(
                new() {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    AddressId = addr.Id,
                    Type = faker.PickRandom(AddressTypes),
                    IsPrimary = isPrimaryAddr.Add(personId),
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        await db.SaveChangesAsync(ct);

        // Create some relationships between random pairs of persons
        var relationships = BuildRelationships(faker, persons);
        if (relationships.Count > 0) {
            db.PersonRelationships.AddRange(relationships);
            await db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Seeded {PersonCount} persons, {PhoneCount} phones, {EmailCount} emails, {AddrCount} addresses, {SocialCount} social profiles, {JobCount} employments, {RelCount} relationships.",
            persons.Count, phones.Count, emails.Count, addrs.Count, socials.Count, jobs.Count, relationships.Count);
    }

    private PeopleDbContext CreateContext()
    {
        var connStr = _connStr.GetOrThrow();
        var opts = new DbContextOptionsBuilder<PeopleDbContext>().UseNpgsql(connStr, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "people")).Options;
        return new(opts);
    }

    private static List<PersonEntity> BuildPersons(Faker faker, int count)
    {
        var personFaker = new Faker<PersonEntity>().RuleFor(p => p.Id, _ => Guid.NewGuid())
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.MiddleName, f => f.Random.Bool(0.4f) ? f.Name.FirstName() : null)
            .RuleFor(p => p.PreferredName, f => f.Random.Bool(0.2f) ? f.Name.FirstName() : null)
            .RuleFor(p => p.DateOfBirth, f => f.Random.Bool(0.8f) ? DateOnly.FromDateTime(f.Date.Between(new(1950, 1, 1), new(2005, 1, 1))) : null)
            .RuleFor(p => p.Sex, f => f.Random.Bool(0.7f) ? f.PickRandom("M", "F") : null)
            .RuleFor(p => p.Nationality, f => f.Random.Bool(0.7f) ? f.PickRandom(Nationalities) : null)
            .RuleFor(p => p.PreferredLanguageBcp47, f => f.Random.Bool(0.6f) ? f.PickRandom(Languages) : null)
            .RuleFor(p => p.CurrentJobTitle, f => f.Random.Bool(0.6f) ? f.Name.JobTitle() : null)
            .RuleFor(p => p.CurrentCompany, f => f.Random.Bool(0.5f) ? f.Company.CompanyName() : null)
            .RuleFor(p => p.IsActive, f => f.Random.Bool(0.9f))
            .RuleFor(p => p.Source, _ => "Seed")
            .RuleFor(p => p.CreatedTimestamp, f => f.Date.Past(3).ToUniversalTime());

        return personFaker.Generate(count);
    }

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
            Platform = f.PickRandom(SocialPlatforms),
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