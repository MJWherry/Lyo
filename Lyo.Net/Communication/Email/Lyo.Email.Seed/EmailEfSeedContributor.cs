using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Email.Postgres;
using Lyo.Email.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Email.Seed;

/// <summary>EF seed of outbound email logs, each with one attachment log. Schema <c>email</c>.</summary>
public sealed class EmailEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Email";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresEmailOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<EmailDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new EmailDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        graph.Entity(
                options.Count, i => new EmailLogEntity {
                    Id = LyoGuid.CreateV7(),
                    Direction = EmailDirection.Outbound,
                    FromAddress = faker.Internet.Email(),
                    FromName = faker.Name.FullName(),
                    ToAddressesJson = $"[\"{faker.Internet.UserName()}.{i}@example.com\"]",
                    Subject = faker.Lorem.Sentence(4).TrimEnd('.'),
                    TextBody = faker.Lorem.Paragraph(),
                    IsSuccess = true,
                    Message = "accepted",
                    MessageId = $"mail-{i}@example.com",
                    SentTimestamp = now,
                    CreatedTimestamp = now
                })
            .ForEach(
                _ => 1, (log, _) => new EmailAttachmentLogEntity {
                    Id = LyoGuid.CreateV7(),
                    EmailLogId = log.Id,
                    FileName = "notes.txt",
                    ContentType = "text/plain",
                    SizeBytes = 4,
                    SortOrder = 1,
                    CreatedTimestamp = now
                });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<EmailDbContext> ef)
            throw new SeedException("Email OnClear requires EfSeedTransport<EmailDbContext>.");

        await ef.Context.EmailAttachmentLogs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await ef.Context.EmailLogs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}