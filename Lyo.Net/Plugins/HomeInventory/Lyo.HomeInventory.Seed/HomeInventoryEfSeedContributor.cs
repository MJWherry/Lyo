using Bogus;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Lyo.HomeInventory.Postgres;
using Lyo.HomeInventory.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.HomeInventory.Seed;

/// <summary>EF seed of several categories and locations, then items with one stock row, one receipt, and one attachment each. Schema <c>home_inventory</c>.</summary>
public sealed class HomeInventoryEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <inheritdoc />
    public override string Name => "HomeInventory";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresHomeInventoryOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<HomeInventoryDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new HomeInventoryDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var lookupCount = Math.Min(5, Math.Max(1, options.Count));
        var categories = Enumerable.Range(0, lookupCount).Select(i => {
            var name = faker.Commerce.Department();
            return new HomeCategoryEntity {
                Id = LyoGuid.CreateV7(),
                Name = name,
                Slug = $"{Slug(name)}-{i}",
                CreatedTimestamp = now
            };
        }).ToArray();
        var locations = Enumerable.Range(0, lookupCount).Select(i => new HomeLocationEntity {
            Id = LyoGuid.CreateV7(),
            Name = faker.Address.StreetName(),
            Code = $"L{i}",
            IsActive = true,
            CreatedTimestamp = now
        }).ToArray();
        graph.Entity(categories);
        graph.Entity(locations);
        var items = graph.Entity(options.Count, i => new HomeItemEntity {
            Id = LyoGuid.CreateV7(),
            CategoryId = categories[i % lookupCount].Id,
            LocationId = locations[i % lookupCount].Id,
            Name = faker.Commerce.ProductName(),
            ProductKeysJson = "[]",
            CreatedTimestamp = now
        });
        items.ForEach(_ => 1, (item, _) => new HomeItemStockEntity {
            ItemId = item.Id,
            LocationId = item.LocationId!.Value,
            QuantityOnHand = faker.Random.Int(1, 24),
            UpdatedTimestamp = now
        });
        items.ForEach(_ => 1, (item, _) => new HomeItemMovementEntity {
            Id = LyoGuid.CreateV7(),
            ItemId = item.Id,
            MovementType = (int)HomeItemMovementType.Receipt,
            Quantity = 1,
            ToLocationId = item.LocationId,
            CreatedTimestamp = now
        });
        items.ForEach(_ => 1, (item, _) => new HomeAttachmentEntity {
            Id = LyoGuid.CreateV7(),
            EntityType = HomeInventoryEntityTypes.Item,
            EntityId = item.Id,
            FileId = LyoGuid.CreateV7(),
            Caption = faker.Commerce.ProductAdjective(),
            IsPrimary = true,
            CreatedTimestamp = now
        });
    }

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant().Select(ch => char.IsAsciiLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = new string(chars).Trim('-');
        return slug.Length == 0 ? "item" : slug;
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<HomeInventoryDbContext> ef)
            throw new SeedException("HomeInventory OnClear requires EfSeedTransport<HomeInventoryDbContext>.");

        var db = ef.Context;
        await db.Attachments.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Movements.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Stocks.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Items.ExecuteUpdateAsync(s => s.SetProperty(i => i.ParentItemId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Items.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Locations.ExecuteUpdateAsync(s => s.SetProperty(i => i.ParentLocationId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Locations.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Categories.ExecuteUpdateAsync(s => s.SetProperty(i => i.ParentCategoryId, (Guid?)null), ct).ConfigureAwait(false);
        await db.Categories.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
