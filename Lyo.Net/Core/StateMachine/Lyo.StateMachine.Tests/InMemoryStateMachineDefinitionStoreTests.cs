namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class InMemoryStateMachineDefinitionStoreTests
{
    private static StateMachineDocument Doc(string initial) => new() { Name = "job", InitialState = initial, States = [new() { Name = initial }] };

    [Fact]
    public async Task SaveAsync_StaleRevision_ReturnsFalse()
    {
        var store = new InMemoryStateMachineDefinitionStore();
        Assert.True(await store.SaveAsync(Doc("A"), 0, TestCt));
        Assert.False(await store.SaveAsync(Doc("B"), 0, TestCt));
        Assert.True(await store.SaveAsync(Doc("B"), 1, TestCt));
        Assert.Equal([1, 2], await store.ListRevisionsAsync("job", TestCt));
        Assert.Equal(["job"], await store.ListAsync(TestCt));
    }

    [Fact]
    public async Task GetAsync_NoRevision_ReturnsLatest()
    {
        var store = new InMemoryStateMachineDefinitionStore();
        await store.SaveAsync(Doc("A") with { Revision = 99 }, 0, TestCt);
        await store.SaveAsync(Doc("B"), 1, TestCt);
        var latest = await store.GetAsync("job", ct: TestCt);
        var first = await store.GetAsync("job", 1, TestCt);
        Assert.Equal(("B", 2), (latest!.InitialState, latest.Revision));
        Assert.Equal(("A", 1), (first!.InitialState, first.Revision));
        Assert.Null(await store.GetAsync("missing", ct: TestCt));
        Assert.True(await store.DeleteAsync("job", TestCt));
        Assert.Null(await store.GetAsync("job", ct: TestCt));
    }
}