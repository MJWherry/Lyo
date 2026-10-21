using Lyo.Common.Core.Identifiers;
using Lyo.StateMachine.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.StateMachine.Postgres.Tests;

public class PostgresStateInstanceQueryTests(StateMachinePostgresFixture fixture)
{
    private static readonly DateTime T0 = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    private IStateStore<OrderState> Store => fixture.ServiceProvider.GetRequiredService<IStateStore<OrderState>>();

    private IStateInstanceQuery<OrderState> Query => fixture.ServiceProvider.GetRequiredService<IStateInstanceQuery<OrderState>>();

    [Fact]
    public void Query_IsSameSingletonAsStore() => Assert.Same(Store, Query);

    [Fact]
    public async Task ListAsync_DueBefore_ReturnsOnlyDue()
    {
        var machine = NewMachine();
        await Store.SaveAsync(machine, "a", new(OrderState.Paid, 1, T0, T0.AddSeconds(10)), 0, ct: TestCt);
        await Store.SaveAsync(machine, "b", new(OrderState.Paid, 1, T0, T0.AddSeconds(5)), 0, ct: TestCt);
        await Store.SaveAsync(machine, "c", new(OrderState.Shipped, 1, T0), 0, ct: TestCt);
        var dueSoon = await Query.ListAsync(machine, new() { DueBeforeUtc = T0.AddSeconds(7) }, ct: TestCt);
        var dueLater = await Query.ListAsync(machine, new() { DueBeforeUtc = T0.AddSeconds(20) }, ct: TestCt);
        var shipped = await Query.ListAsync(machine, new() { States = [OrderState.Shipped] }, ct: TestCt);
        var paged = await Query.ListAsync(machine, skip: 1, take: 1, ct: TestCt);
        Assert.Equal(["b"], dueSoon.Select(i => i.InstanceKey));
        Assert.Equal(["b", "a"], dueLater.Select(i => i.InstanceKey));
        Assert.Equal(T0.AddSeconds(5), dueLater[0].Snapshot.DueAtUtc);
        Assert.Equal(["c"], shipped.Select(i => i.InstanceKey));
        Assert.Equal(["b"], paged.Select(i => i.InstanceKey));
    }

    [Fact]
    public async Task CountByStateAsync_ReturnsCounts()
    {
        var machine = NewMachine();
        await Store.SaveAsync(machine, "a", new(OrderState.Paid, 1, T0), 0, ct: TestCt);
        await Store.SaveAsync(machine, "b", new(OrderState.Paid, 1, T0), 0, ct: TestCt);
        await Store.SaveAsync(machine, "c", new(OrderState.Shipped, 1, T0), 0, ct: TestCt);
        var counts = await Query.CountByStateAsync(machine, TestCt);
        Assert.Equal(new Dictionary<OrderState, int> { [OrderState.Paid] = 2, [OrderState.Shipped] = 1 }, counts);
    }

    [Fact]
    public async Task GetHistoryAsync_PagesOldestFirst()
    {
        var machine = NewMachine();
        await SeedHistoryAsync(machine, "a");
        var all = await Query.GetHistoryAsync(machine, "a", ct: TestCt);
        var second = await Query.GetHistoryAsync(machine, "a", 1, 1, TestCt);
        Assert.Equal([1L, 2L, 3L], all.Select(r => r.Version));
        Assert.Equal((OrderState.Paid, OrderState.Shipped, "Ship", 2L), (second.Single().From, second.Single().To, second.Single().Trigger, second.Single().Version));
    }

    [Fact]
    public async Task DeleteAsync_RemovesInstanceAndHistory()
    {
        var machine = NewMachine();
        await SeedHistoryAsync(machine, "a");
        Assert.False(await Query.DeleteAsync(machine, "a", 1, TestCt));
        Assert.True(await Query.DeleteAsync(machine, "a", 3, TestCt));
        Assert.False(await Query.DeleteAsync(machine, "a", ct: TestCt));
        Assert.Null(await Store.LoadAsync(machine, "a", TestCt));
        Assert.Empty(await Query.GetHistoryAsync(machine, "a", ct: TestCt));
    }

    private async Task SeedHistoryAsync(string machine, string key)
    {
        await Store.SaveAsync(machine, key, new(OrderState.Paid, 1, T0), 0, new(OrderState.Pending, OrderState.Paid, "Pay", 1, T0), TestCt);
        await Store.SaveAsync(machine, key, new(OrderState.Shipped, 2, T0), 1, new(OrderState.Paid, OrderState.Shipped, "Ship", 2, T0.AddSeconds(1)), TestCt);
        await Store.SaveAsync(machine, key, new(OrderState.Paid, 3, T0), 2, new(OrderState.Shipped, OrderState.Paid, "Return", 3, T0.AddSeconds(2)), TestCt);
    }

    private static string NewMachine() => $"query-{LyoGuid.CreateV7():N}";
}