using Lyo.Rating;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Rating.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class RatingPostgresStoreTests(RatingPostgresFixture fixture)
{
    [Fact]
    public async Task Save_ThenGetById_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = fixture.ServiceProvider.GetRequiredService<IRatingStore>();
        var subject = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var row = new RatingRecord {
            Id = Guid.NewGuid(),
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
