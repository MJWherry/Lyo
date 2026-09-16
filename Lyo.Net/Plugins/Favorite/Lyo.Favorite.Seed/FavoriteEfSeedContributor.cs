using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions;
using Lyo.Favorite.Postgres;
using Lyo.Favorite.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Favorite.Seed;

/// <summary>EF seed of favorite rows, each from a different person on a different item.</summary>
public sealed class FavoriteEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Tenant id written on every seeded row.</summary>
    public static readonly Guid TenantId = new("11111111-1111-7111-8111-111111111111");

    /// <summary>Subject type written on every seeded row.</summary>
    public const string SubjectType = "item";

    /// <inheritdoc />
    public override string Name => "Favorite";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresFavoriteOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<FavoriteDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new FavoriteDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        graph.Entity(options.Count, i => new FavoriteEntity {
            Id = LyoGuid.CreateV7(),
            TenantId = TenantId,
            SubjectEntityType = SubjectType,
            SubjectEntityId = $"item-{i}",
            ActorEntityType = "person",
            ActorEntityId = $"person-{i}",
            Visibility = EntityRefVisibility.Public,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<FavoriteDbContext> ef)
            throw new SeedException("Favorite OnClear requires EfSeedTransport<FavoriteDbContext>.");

        await ef.Context.Favorites.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
