namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class InMemoryStateStoreQueryTests
{
    private static readonly DateTime T0 = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<InMemoryStateStore<JobState>> SeededAsync()
    {
        var store = new InMemoryStateStore<JobState>();
        await store.SaveAsync("job", "a", new(JobState.Running, 1, T0, T0.AddSeconds(10)), 0, ct: TestCt);
        await store.SaveAsync("job", "b", new(JobState.Running, 1, T0, T0.AddSeconds(5)), 0, ct: TestCt);
        await store.SaveAsync("job", "c", new(JobState.Done, 1, T0), 0, ct: TestCt);
        await store.SaveAsync("other", "a", new(JobState.Running, 1, T0), 0, ct: TestCt);
        return store;
    }

    [Fact]
    public async Task ListAsync_FiltersByStatesAndDue()
    {
        var store = await SeededAsync();
        var running = await store.ListAsync("job", new() { States = [JobState.Running] }, ct: TestCt);
        var dueSoon = await store.ListAsync("job", new() { DueBeforeUtc = T0.AddSeconds(7) }, ct: TestCt);
        var dueLater = await store.ListAsync("job", new() { DueBeforeUtc = T0.AddSeconds(20) }, ct: TestCt);
        var paged = await store.ListAsync("job", skip: 1, take: 1, ct: TestCt);
        Assert.Equal(["a", "b"], running.Select(i => i.InstanceKey));
        Assert.Equal(["b"], dueSoon.Select(i => i.InstanceKey));
        Assert.Equal(["b", "a"], dueLater.Select(i => i.InstanceKey));
        Assert.Equal(["b"], paged.Select(i => i.InstanceKey));
    }

    [Fact]
    public async Task CountByStateAsync_GroupsCounts()
    {
        var store = await SeededAsync();
        var counts = await store.CountByStateAsync("job", TestCt);
        Assert.Equal(new Dictionary<JobState, int> { [JobState.Running] = 2, [JobState.Done] = 1 }, counts);
    }

    [Fact]
    public async Task DeleteAsync_StaleVersion_ReturnsFalse()
    {
        var store = await SeededAsync();
        Assert.False(await store.DeleteAsync("job", "a", 5, TestCt));
        Assert.True(await store.DeleteAsync("job", "a", 1, TestCt));
        Assert.False(await store.DeleteAsync("job", "a", ct: TestCt));
        Assert.Null(await store.LoadAsync("job", "a", TestCt));
        Assert.Empty(await store.GetHistoryAsync("job", "a", ct: TestCt));
    }
}