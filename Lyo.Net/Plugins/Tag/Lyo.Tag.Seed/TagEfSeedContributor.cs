using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Seed;
using Lyo.Tag.Postgres;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Tag.Seed;

/// <summary>EF seed of tags, each on a different item from a different person.</summary>
public sealed class TagEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Subject type written on every seeded row.</summary>
    public const string SubjectType = "item";

    /// <summary>Tenant id written on every seeded row.</summary>
    public static readonly Guid TenantId = new("11111111-1111-7111-8111-111111111111");

    /// <inheritdoc />
    public override string Name => "Tag";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresTagOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<TagDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new TagDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        graph.Entity(
            options.Count, i => {
                var name = faker.Commerce.ProductAdjective();
                return new TagEntity {
                    Id = LyoGuid.CreateV7(),
                    TenantId = TenantId,
                    SubjectEntityType = SubjectType,
                    SubjectEntityId = $"item-{i}",
                    ActorEntityType = "person",
                    ActorEntityId = $"person-{i}",
                    Visibility = EntityRefVisibility.Public,
                    CreatedAt = DateTime.UtcNow,
                    Name = name,
                    TagType = "tag",
                    Slug = $"{SlugOf(name)}-{i}"
                };
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<TagDbContext> ef)
            throw new SeedException("Tag OnClear requires EfSeedTransport<TagDbContext>.");

        await ef.Context.Tags.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }

    private static string SlugOf(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        return new string(chars).Trim('-');
    }
}