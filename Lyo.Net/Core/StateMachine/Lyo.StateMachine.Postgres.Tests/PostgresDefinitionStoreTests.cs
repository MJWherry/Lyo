using Lyo.Common.Core.Identifiers;
using Lyo.StateMachine.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.StateMachine.Postgres.Tests;

public sealed class OrderContext
{
    public decimal Paid { get; init; }

    public decimal Total { get; init; }
}

public class PostgresDefinitionStoreTests(StateMachinePostgresFixture fixture)
{
    private IStateMachineDefinitionStore Store => fixture.ServiceProvider.GetRequiredService<IStateMachineDefinitionStore>();

    private static StateMachineDocument Order(string name, string initial = "Pending")
        => new() {
            Name = name,
            InitialState = initial,
            States = [
                new() {
                    Name = "Pending",
                    Transitions = [new() { Kind = TransitionDocumentKind.Permit, Trigger = "Pay", Destination = "Paid", Guard = "paid in full" }]
                },
                new() {
                    Name = "Paid",
                    Timers = [new() { After = TimeSpan.FromDays(2), Trigger = "Ship" }],
                    Transitions = [new() { Kind = TransitionDocumentKind.Permit, Trigger = "Ship", Destination = "Shipped" }]
                },
                new() { Name = "Shipped", IsFinal = true }
            ]
        };

    [Fact]
    public async Task SaveAsync_AppendsRevision()
    {
        var name = NewName();

        Assert.True(await Store.SaveAsync(Order(name), 0, TestCt));
        Assert.True(await Store.SaveAsync(Order(name, "Paid"), 1, TestCt));

        Assert.Equal([1, 2], await Store.ListRevisionsAsync(name, TestCt));
        Assert.Contains(name, await Store.ListAsync(TestCt));
    }

    [Fact]
    public async Task SaveAsync_StaleRevision_ReturnsFalse()
    {
        var name = NewName();
        Assert.True(await Store.SaveAsync(Order(name), 0, TestCt));

        Assert.False(await Store.SaveAsync(Order(name), 0, TestCt));
        Assert.False(await Store.SaveAsync(Order(name), 5, TestCt));
        Assert.Equal([1], await Store.ListRevisionsAsync(name, TestCt));
    }

    [Fact]
    public async Task GetAsync_SpecificRevision()
    {
        var name = NewName();
        await Store.SaveAsync(Order(name) with { Revision = 42 }, 0, TestCt);
        await Store.SaveAsync(Order(name, "Paid"), 1, TestCt);

        var first = await Store.GetAsync(name, 1, TestCt);
        var latest = await Store.GetAsync(name, ct: TestCt);

        Assert.Equal(StateMachineDocumentJson.Serialize(Order(name) with { Revision = 1 }), StateMachineDocumentJson.Serialize(first!));
        Assert.Equal(("Paid", 2), (latest!.InitialState, latest.Revision));
        Assert.Null(await Store.GetAsync(name, 3, TestCt));
        Assert.True(await Store.DeleteAsync(name, TestCt));
        Assert.Null(await Store.GetAsync(name, ct: TestCt));
        Assert.False(await Store.DeleteAsync(name, TestCt));
    }

    [Fact]
    public async Task LoadedFromStore_FiresEndToEnd()
    {
        var name = NewName();
        await Store.SaveAsync(Order(name), 0, TestCt);
        var registry = new StateMachineRegistry<OrderState, string, OrderContext>().Guard("paid in full", c => c.Paid >= c.Total);
        var cache = new StateMachineDefinitionCache<OrderState, string, OrderContext>(Store, registry, EnumStateKeyConverter<OrderState>.Instance, StringStateKeyConverter.Instance);
        var definition = await cache.GetAsync(name, ct: TestCt);
        var states = fixture.ServiceProvider.GetRequiredService<IStateStore<OrderState>>();
        var instance = new StateMachineInstance<OrderState, string, OrderContext>(definition, states, "order-1");

        var rejected = await instance.FireAsync(new() { Paid = 5, Total = 10 }, "Pay", TestCt);
        var paid = await instance.FireAsync(new() { Paid = 10, Total = 10 }, "Pay", TestCt);

        Assert.Equal(TransitionRejection.GuardFailed, rejected.Rejection);
        Assert.Equal(OrderState.Paid, paid.To);
        var saved = await states.LoadAsync(name, "order-1", TestCt);
        Assert.Equal((OrderState.Paid, 1L), (saved!.State, saved.Version));
        Assert.Equal(saved.EnteredAtUtc.AddDays(2), saved.DueAtUtc);
    }

    private static string NewName() => $"order-{LyoGuid.CreateV7():N}";
}
