using Lyo.Favorite;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Favorite.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class FavoritePostgresStoreTests(FavoritePostgresFixture fixture)
{
    [Fact]
    public async Task Save_ThenGetById_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = fixture.ServiceProvider.GetRequiredService<IFavoriteStore>();
        var subject = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var row = new FavoriteRecord {
            Id = Guid.NewGuid(),
            SubjectEntityType = "thing",
            SubjectEntityId = subject.ToString(),
            ActorEntityType = "person",
            ActorEntityId = actor.ToString(),
        };
        await store.SaveAsync(row, ct: ct);
        Assert.NotEqual(Guid.Empty, row.Id);
        var loaded = await store.GetByIdAsync(row.Id, ct: ct);
        Assert.NotNull(loaded);
        Assert.Equal(row.Id, loaded.Id);
        Assert.Equal("thing", loaded.SubjectEntityType);
        Assert.Equal(subject.ToString(), loaded.SubjectEntityId);

    }
}
