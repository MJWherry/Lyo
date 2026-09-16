using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Rating.Postgres;
using Lyo.Rating.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Rating.Seed;

/// <summary>EF seed of ratings on different products, each from a different person and with one like.</summary>
public sealed class RatingEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Tenant id written on every seeded row.</summary>
    public static readonly Guid TenantId = new("11111111-1111-7111-8111-111111111111");

    /// <summary>Subject type written on every seeded row.</summary>
    public const string SubjectType = "product";

    /// <inheritdoc />
    public override string Name => "Rating";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresRatingOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<RatingDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new RatingDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        graph.Entity(options.Count, i => new RatingEntity {
            Id = LyoGuid.CreateV7(),
            TenantId = TenantId,
            SubjectEntityType = SubjectType,
            SubjectEntityId = $"product-{i}",
            ActorEntityType = "person",
            ActorEntityId = $"person-{i}",
            Visibility = EntityRefVisibility.Public,
            CreatedAt = DateTime.UtcNow,
            Subject = faker.Lorem.Word(),
            Title = faker.Lorem.Sentence(3).TrimEnd('.'),
            Value = faker.Random.Decimal(1, 5),
            Message = faker.Lorem.Sentence()
        }).ForEach(_ => 1, (rating, _) => new RatingReactionEntity {
            Id = LyoGuid.CreateV7(),
            ReactionType = (int)RatingReactionType.Like,
            TenantId = rating.TenantId,
            SubjectEntityType = rating.SubjectEntityType,
            SubjectEntityId = rating.SubjectEntityId,
            ActorEntityType = rating.ActorEntityType,
            ActorEntityId = rating.ActorEntityId,
            CreatedTimestamp = DateTime.UtcNow
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<RatingDbContext> ef)
            throw new SeedException("Rating OnClear requires EfSeedTransport<RatingDbContext>.");

        var db = ef.Context;
        await db.RatingReactions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Ratings.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
