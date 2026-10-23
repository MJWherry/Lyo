using Lyo.Common.Core.Identifiers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Favorite.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class FavoritePostgresStoreTests(FavoritePostgresFixture fixture)
{
    [Fact]
    public async Task Save_ThenGetById_RoundTrips()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IFavoriteStore>();
        var subject = LyoGuid.CreateV7();
        var actor = LyoGuid.CreateV7();
        var row = new FavoriteRecord {
            Id = LyoGuid.CreateV7(),
            SubjectEntityType = "thing",
            SubjectEntityId = subject.ToString(),
            ActorEntityType = "person",
            ActorEntityId = actor.ToString()
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