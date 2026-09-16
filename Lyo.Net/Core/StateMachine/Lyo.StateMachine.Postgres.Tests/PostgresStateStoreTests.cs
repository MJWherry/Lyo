using System.Text.Json.Nodes;
using Lyo.Common.Core.Identifiers;
using Lyo.StateMachine.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.StateMachine.Postgres.Tests;

public class PostgresStateStoreTests(StateMachinePostgresFixture fixture)
{
    private const string Machine = "order";
    private static readonly DateTime EnteredAt = new(2026, 9, 26, 12, 30, 0, DateTimeKind.Utc);

    private IStateStore<OrderState> Store => fixture.ServiceProvider.GetRequiredService<IStateStore<OrderState>>();

    [Fact]
    public async Task DbContext_MigrationsApplied_NoPendingMigrations()
    {
        await using var context = await fixture.ContextFactory.CreateDbContextAsync(TestCt);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestCt));
    }

    [Fact]
    public async Task LoadAsync_Missing_ReturnsNull() => Assert.Null(await Store.LoadAsync(Machine, NewKey(), TestCt));

    [Fact]
    public async Task SaveAsync_RoundTripsEnumStateAndVersion()
    {
        var key = NewKey();

        Assert.True(await Store.SaveAsync(Machine, key, new(OrderState.Paid, 1, EnteredAt), 0, ct: TestCt));
        Assert.True(await Store.SaveAsync(Machine, key, new(OrderState.Shipped, 2, EnteredAt.AddMinutes(5)), 1, ct: TestCt));

        var loaded = await Store.LoadAsync(Machine, key, TestCt);
        Assert.Equal(new StateSnapshot<OrderState>(OrderState.Shipped, 2, EnteredAt.AddMinutes(5)), loaded);
        await using var context = await fixture.ContextFactory.CreateDbContextAsync(TestCt);
        var row = await context.Instances.AsNoTracking().SingleAsync(i => i.MachineName == Machine && i.InstanceKey == key, TestCt);
        Assert.Equal("Shipped", row.State);
    }

    [Fact]
    public async Task SaveAsync_StaleVersion_ReturnsConflict()
    {
        var key = NewKey();
        Assert.True(await Store.SaveAsync(Machine, key, new(OrderState.Paid, 1, EnteredAt), 0, ct: TestCt));

        var duplicateInsert = await Store.SaveAsync(Machine, key, new(OrderState.Shipped, 1, EnteredAt), 0, ct: TestCt);
        var staleUpdate = await Store.SaveAsync(Machine, key, new(OrderState.Shipped, 6, EnteredAt), 5, ct: TestCt);

        Assert.False(duplicateInsert);
        Assert.False(staleUpdate);
        Assert.Equal(new StateSnapshot<OrderState>(OrderState.Paid, 1, EnteredAt), await Store.LoadAsync(Machine, key, TestCt));
    }

    [Fact]
    public async Task SaveAsync_WritesTransitionHistoryRow()
    {
        var key = NewKey();
        var record = new TransitionRecord<OrderState>(OrderState.Pending, OrderState.Paid, "Pay", 1, EnteredAt);

        await Store.SaveAsync(Machine, key, new(OrderState.Paid, 1, EnteredAt), 0, record, TestCt);
        await Store.SaveAsync(Machine, key, new(OrderState.Shipped, 1, EnteredAt), 0, record with { To = OrderState.Shipped }, TestCt);

        await using var context = await fixture.ContextFactory.CreateDbContextAsync(TestCt);
        var rows = await context.Transitions.AsNoTracking().Where(t => t.MachineName == Machine && t.InstanceKey == key).ToListAsync(TestCt);
        var row = Assert.Single(rows);
        Assert.Equal(("Pending", "Paid", "Pay", 1L, EnteredAt), (row.FromState, row.ToState, row.Trigger, row.Version, row.OccurredTimestamp));
    }

    [Fact]
    public async Task SaveAsync_RoundTripsDueAtAndData()
    {
        var key = NewKey();
        var snapshot = new StateSnapshot<OrderState>(OrderState.Paid, 1, EnteredAt, EnteredAt.AddMinutes(10), """{"percent":40,"notes":["a"]}""");
        Assert.True(await Store.SaveAsync(Machine, key, snapshot, 0, ct: TestCt));
        Assert.True(await Store.SaveAsync(Machine, key, snapshot with { Version = 2, DueAtUtc = null }, 1, ct: TestCt));

        var loaded = await Store.LoadAsync(Machine, key, TestCt);

        Assert.Equal((OrderState.Paid, 2L, EnteredAt, (DateTime?)null), (loaded!.State, loaded.Version, loaded.EnteredAtUtc, loaded.DueAtUtc));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(snapshot.DataJson!), JsonNode.Parse(loaded.DataJson!)));
    }

    [Fact]
    public async Task SaveAsync_NonUtc_Throws()
    {
        var unspecified = new StateSnapshot<OrderState>(OrderState.Paid, 1, new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
        var localDue = new StateSnapshot<OrderState>(OrderState.Paid, 1, EnteredAt, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local));

        await Assert.ThrowsAsync<ArgumentException>(() => Store.SaveAsync(Machine, NewKey(), unspecified, 0, ct: TestCt));
        await Assert.ThrowsAsync<ArgumentException>(() => Store.SaveAsync(Machine, NewKey(), localDue, 0, ct: TestCt));
    }

    [Fact]
    public void AddStateMachinePostgresFromConfiguration_OnlyConnectionString_UsesDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"{PostgresStateMachineOptions.SectionName}:ConnectionString"] = "Host=localhost" })
            .Build();

        var services = new ServiceCollection().AddStateMachinePostgresFromConfiguration(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<PostgresStateMachineOptions>();
        Assert.Equal("Host=localhost", options.ConnectionString);
        Assert.False(options.EnableAutoMigrations);
    }

    [Fact]
    public void AddStateMachinePostgresFromConfiguration_MissingSection_Throws()
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddStateMachinePostgresFromConfiguration(new ConfigurationBuilder().Build()));

    private static string NewKey() => LyoGuid.CreateV7().ToString("N");
}
