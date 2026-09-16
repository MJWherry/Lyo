using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Lyo.Sms.Postgres;
using Lyo.Sms.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Sms.Seed;

/// <summary>EF seed of outbound SMS logs. Schema <c>sms</c> on <see cref="SmsDbContext" />.</summary>
public sealed class SmsEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "SMS";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresSmsOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<SmsDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new SmsDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        graph.Entity(options.Count, i => new SmsLogEntity {
            Id = LyoGuid.CreateV7(),
            To = $"+1412{5550100 + i}",
            From = "+14125550199",
            Body = faker.Lorem.Sentence(),
            IsSuccess = true,
            Message = "accepted",
            Status = "sent",
            MessageId = $"sms-{i}",
            DateSent = now,
            CreatedAt = now
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<SmsDbContext> ef)
            throw new SeedException("SMS OnClear requires EfSeedTransport<SmsDbContext>.");

        await ef.Context.SmsLogs.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
