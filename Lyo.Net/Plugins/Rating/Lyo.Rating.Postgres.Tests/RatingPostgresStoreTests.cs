using Lyo.Rating;
using Microsoft.Extensions.DependencyInjection;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Rating.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class RatingPostgresStoreTests(RatingPostgresFixture fixture)
{
    [Fact]
    public async Task Save_ThenGetById_RoundTrips()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<IRatingStore>();
        var subject = LyoGuid.CreateV7();
        var actor = LyoGuid.CreateV7();
        var row = new RatingRecord {
            Id = LyoGuid.CreateV7(),
            SubjectEntityType = "thing",
            SubjectEntityId = subject.ToString(),
            ActorEntityType = "person",
            ActorEntityId = actor.ToString(),
            Value = 4.5m,
        };
        await store.SaveAsync(row, ct: ct);
        Assert.NotEqual(Guid.Empty, row.Id);
        var loaded = await store.GetByIdAsync(row.Id, ct: ct);
        Assert.NotNull(loaded);
        Assert.Equal(row.Id, loaded.Id);
        Assert.Equal("thing", loaded.SubjectEntityType);
        Assert.Equal(subject.ToString(), loaded.SubjectEntityId);
        Assert.Equal(4.5m, loaded.Value);
    }
}
