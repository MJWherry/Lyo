using Lyo.Common.Core.Identifiers;
using Lyo.Diagram.Models;

namespace Lyo.Diagram.Tests;

public class InMemoryDiagramStoreTests
{
    [Fact]
    public async Task InMemoryStore_SaveGetListDelete_RoundTripsDocument()
    {
        var store = new InMemoryDiagramStore();
        var bravo = Sample("Bravo", "end");
        var alpha = Sample("Alpha", "start");
        await store.SaveAsync(bravo, TestCt);
        await store.SaveAsync(alpha, TestCt);

        var loaded = await store.GetAsync(alpha.Id, TestCt);
        Assert.NotNull(loaded);
        Assert.Equal("Start", loaded.Pages[0].Cells.Single(c => c.Id == "start").Value);

        var listed = await store.ListAsync(TestCt);
        Assert.Equal(["Alpha", "Bravo"], listed.Select(s => s.Name).ToArray());
        Assert.Equal(alpha.Id, listed[0].Id);
        Assert.NotEqual(default, listed[0].UpdatedTimestamp);

        var updated = alpha with {
            Pages = [
                alpha.Pages[0] with {
                    Cells = [alpha.Pages[0].Cells[0] with { Value = "Started" }]
                }
            ]
        };
        await store.SaveAsync(updated, TestCt);
        var reloaded = await store.GetAsync(alpha.Id, TestCt);
        Assert.Equal("Started", reloaded!.Pages[0].Cells.Single(c => c.Id == "start").Value);

        Assert.True(await store.DeleteAsync(alpha.Id, TestCt));
        Assert.Null(await store.GetAsync(alpha.Id, TestCt));
        Assert.False(await store.DeleteAsync(alpha.Id, TestCt));
    }

    private static DiagramDocument Sample(string name, string cellId)
        => new() {
            Id = LyoGuid.CreateV7(),
            Name = name,
            Pages = [
                new DiagramPage {
                    Id = "page",
                    Name = "Page",
                    Cells = [new() { Id = cellId, Value = "Start", IsVertex = true }]
                }
            ]
        };
}
