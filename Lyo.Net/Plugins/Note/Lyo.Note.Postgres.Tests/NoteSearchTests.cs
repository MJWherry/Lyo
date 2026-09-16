using Lyo.Common.Core.Identifiers;
using Lyo.Note;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Note.Postgres.Tests;

public sealed class NoteSearchTests(NotePostgresFixture fixture)
{
    [Fact]
    public async Task Search_Note_MatchesBodyTerm_AndSkipsOtherNote()
    {
        var store = fixture.ServiceProvider.GetRequiredService<INoteStore>();
        var match = await Save(store, "the widget manual");
        var other = await Save(store, "unrelated ledger");

        var found = await store.SearchAsync("widget", ct: TestCt);

        Assert.Contains(found, note => note.Id == match);
        Assert.DoesNotContain(found, note => note.Id == other);
    }

    private static async Task<Guid> Save(INoteStore store, string content)
    {
        var id = LyoGuid.CreateV7();
        await store.SaveAsync(new NoteRecord {
            Id = id,
            SubjectEntityType = "thing",
            SubjectEntityId = LyoGuid.CreateV7().ToString(),
            ActorEntityType = "person",
            ActorEntityId = LyoGuid.CreateV7().ToString(),
            Content = content
        }, ct: TestCt);
        return id;
    }
}
