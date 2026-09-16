using Bogus;
using Lyo.Exceptions;
using Lyo.Seed;
using Lyo.ShortUrl.Postgres;
using Lyo.ShortUrl.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.ShortUrl.Seed;

/// <summary>EF seed of active short URLs and one click each, in schema <c>url</c>.</summary>
public sealed class ShortUrlEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "ShortUrl";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresShortUrlOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<ShortUrlDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new ShortUrlDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        graph.Entity(options.Count, i => new ShortUrlEntity {
            Id = $"{faker.Lorem.Word()}-{i}",
            LongUrl = faker.Internet.Url(),
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow
        }).ForEach(_ => 1, (url, _) => new UrlClickEntity {
            ShortUrlId = url.Id,
            ClickedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1"
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<ShortUrlDbContext> ef)
            throw new SeedException("ShortUrl OnClear requires EfSeedTransport<ShortUrlDbContext>.");

        var db = ef.Context;
        await db.UrlClicks.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.ShortUrls.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
