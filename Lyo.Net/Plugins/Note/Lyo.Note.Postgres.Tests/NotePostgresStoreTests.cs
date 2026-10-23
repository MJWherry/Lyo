using Lyo.Common.Core.Identifiers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Note.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class NotePostgresStoreTests(NotePostgresFixture fixture)
{
    [Fact]
    public async Task Save_ThenGetById_RoundTrips()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<INoteStore>();
        var subject = LyoGuid.CreateV7();
        var actor = LyoGuid.CreateV7();
        var row = new NoteRecord {
            Id = LyoGuid.CreateV7(),
            SubjectEntityType = "thing",
            SubjectEntityId = subject.ToString(),
            ActorEntityType = "person",
            ActorEntityId = actor.ToString(),
            Content = "a note"
        };

        await store.SaveAsync(row, ct: ct);
        Assert.NotEqual(Guid.Empty, row.Id);
        var loaded = await store.GetByIdAsync(row.Id, ct: ct);
        Assert.NotNull(loaded);
        Assert.Equal(row.Id, loaded.Id);
        Assert.Equal("thing", loaded.SubjectEntityType);
        Assert.Equal(subject.ToString(), loaded.SubjectEntityId);
        Assert.Equal("a note", loaded.Content);
    }
}