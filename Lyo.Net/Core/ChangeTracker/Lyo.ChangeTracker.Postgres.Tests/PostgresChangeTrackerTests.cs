using System.Text.Json;
using Lyo.Common.Identifiers;

namespace Lyo.ChangeTracker.Postgres.Tests;

public class PostgresChangeTrackerTests
{
    private readonly ChangeTrackerPostgresFixture _fixture;

    public PostgresChangeTrackerTests(ChangeTrackerPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RecordChangeAsync_PersistsAndQueriesByEntity()
    {
        var forEntity = EntityRef.ForKey("Order", "123");
        var fromEntity = EntityRef.ForKey("User", "42");
        var change = new ChangeRecord(forEntity, new Dictionary<string, object?> { ["Status"] = "Draft" }, new Dictionary<string, object?> { ["Status"] = "Submitted" }) {
            FromEntity = fromEntity, ChangeType = "Updated", Message = "Order submitted"
        };

        await _fixture.ChangeTracker.RecordChangeAsync(change, TestContext.Current.CancellationToken);
        var byId = await _fixture.ChangeTracker.GetByIdAsync(change.Id, TestContext.Current.CancellationToken);
        var history = await _fixture.ChangeTracker.GetForEntityAsync(forEntity, TestContext.Current.CancellationToken);
        Assert.NotNull(byId);
        Assert.Single(history);
        Assert.Equal(change.Id, byId.Id);
        Assert.Equal("Updated", byId.ChangeType);
        Assert.Equal("Order submitted", byId.Message);
        Assert.Equal(fromEntity, byId.FromEntity);
        Assert.Equal("Submitted", AsString(byId.ChangedProperties["Status"]));
        Assert.Equal("Draft", AsString(byId.OldValues["Status"]));
    }

    [Fact]
    public async Task GetForEntityTypeAsync_ReturnsNewestFirst()
    {
        var older = new ChangeRecord(EntityRef.ForKey("Order", "A"), new Dictionary<string, object?>(), new Dictionary<string, object?> { ["Status"] = "Draft" }) {
            Timestamp = DateTime.UtcNow.AddMinutes(-10), ChangeType = "Created"
        };

        var newer = new ChangeRecord(
            EntityRef.ForKey("Order", "B"), new Dictionary<string, object?> { ["Status"] = "Draft" }, new Dictionary<string, object?> { ["Status"] = "Submitted" }) {
            Timestamp = DateTime.UtcNow, ChangeType = "Updated"
        };

        await _fixture.ChangeTracker.RecordChangesAsync([older, newer], TestContext.Current.CancellationToken);
        var history = await _fixture.ChangeTracker.GetForEntityTypeAsync("Order", ct: TestContext.Current.CancellationToken);
        Assert.True(history.Count >= 2);
        Assert.Equal(newer.Id, history[0].Id);
        Assert.Equal(older.Id, history[1].Id);
    }

    [Fact]
    public async Task DeleteForEntityAsync_RemovesTrackedHistory()
    {
        var forEntity = EntityRef.ForKey("Invoice", "9001");
        var change = new ChangeRecord(forEntity, new Dictionary<string, object?>(), new Dictionary<string, object?> { ["Status"] = "Paid" });
        await _fixture.ChangeTracker.RecordChangeAsync(change, TestContext.Current.CancellationToken);
        await _fixture.ChangeTracker.DeleteForEntityAsync(forEntity, TestContext.Current.CancellationToken);
        var history = await _fixture.ChangeTracker.GetForEntityAsync(forEntity, TestContext.Current.CancellationToken);
        Assert.Empty(history);
    }

    private static string? AsString(object? value)
        => value switch {
            null => null,
            JsonElement element => element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString(),
            var _ => value.ToString()
        };
}
