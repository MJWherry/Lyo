using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.ContactUs.Postgres;
using Lyo.ContactUs.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.ContactUs.Seed;

/// <summary>EF seed of contact-form submissions in schema <c>contact</c>.</summary>
public sealed class ContactUsEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "ContactUs";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresContactUsOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<ContactUsDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new ContactUsDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        graph.Entity(
            options.Count, _ => new ContactSubmissionEntity {
                Id = LyoGuid.CreateV7(),
                Name = faker.Name.FullName(),
                Email = faker.Internet.Email(),
                Subject = faker.Lorem.Sentence(4).TrimEnd('.'),
                Message = faker.Lorem.Paragraph(),
                Phone = faker.Phone.PhoneNumber("###-###-####"),
                Company = faker.Company.CompanyName(),
                CreatedTimestamp = DateTime.UtcNow
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<ContactUsDbContext> ef)
            throw new SeedException("ContactUs OnClear requires EfSeedTransport<ContactUsDbContext>.");

        await ef.Context.ContactSubmissions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}