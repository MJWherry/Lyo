using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Lyo.Sms.Twilio.Postgres;
using Lyo.Sms.Twilio.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Sms.Twilio.Seed;

/// <summary>EF seed of Twilio SMS logs. Separate from <c>Lyo.Sms.Seed</c>; both use schema name <c>sms</c> on different contexts.</summary>
public sealed class TwilioSmsEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "Twilio SMS";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresTwilioSmsOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<TwilioSmsDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new TwilioSmsDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        graph.Entity(
            options.Count, i => new TwilioSmsLogEntity {
                Id = "SM" + LyoGuid.CreateV7().ToString("N"),
                To = $"+1412{5550100 + i}",
                From = "+14125550199",
                Body = faker.Lorem.Sentence(),
                IsSuccess = true,
                Message = "accepted",
                Status = "sent",
                Direction = MessageDirection.Outbound,
                NumSegments = 1,
                AccountSid = "AC11111111111111111111111111111111",
                Price = 0.01m,
                PriceUnit = "USD",
                DateSent = now,
                CreatedTimestamp = now
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<TwilioSmsDbContext> ef)
            throw new SeedException("Twilio SMS OnClear requires EfSeedTransport<TwilioSmsDbContext>.");

        await ef.Context.TwilioSmsLogs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}