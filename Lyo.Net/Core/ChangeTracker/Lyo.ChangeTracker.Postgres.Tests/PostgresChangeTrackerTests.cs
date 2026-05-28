using System.Text.Json;
using Lyo.ChangeTracker.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    [Fact]
    public async Task RecordChange_WithTenant_FiltersByTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var changeA = new ChangeRecord(EntityRef.ForKey("Order", "tenant-a-1"), new Dictionary<string, object?>(), new Dictionary<string, object?> { ["x"] = 1 }) {
            TenantId = tenantA
        };

        var changeB = new ChangeRecord(EntityRef.ForKey("Order", "tenant-b-1"), new Dictionary<string, object?>(), new Dictionary<string, object?> { ["x"] = 1 }) {
            TenantId = tenantB
        };

        var changeSystem = new ChangeRecord(EntityRef.ForKey("Order", "system-1"), new Dictionary<string, object?>(), new Dictionary<string, object?> { ["x"] = 1 });
        await _fixture.ChangeTracker.RecordChangesAsync([changeA, changeB, changeSystem], TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var onlyTenantA = await context.Changes.WhereTenant(tenantA).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(onlyTenantA);
        Assert.Equal("tenant-a-1", onlyTenantA[0].ForEntityId);
        var onlySystem = await context.Changes.WhereTenant(null).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(onlySystem);
        Assert.Equal("system-1", onlySystem[0].ForEntityId);
        var tenantAOrSystem = await context.Changes.WhereTenantOrSystem(tenantA).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, tenantAOrSystem.Count);
        Assert.Contains(tenantAOrSystem, e => e.ForEntityId == "tenant-a-1");
        Assert.Contains(tenantAOrSystem, e => e.ForEntityId == "system-1");
    }

    [Fact]
    public async Task SystemOnly_PersistsNullTenantRegardlessOfCaller()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>();
        var tracker = new PostgresChangeTracker(
            factory, Options.Create(new EntityRefOptions()), Options.Create(new PostgresChangeTrackerOptions { Tenancy = new() { Mode = TenancyMode.SystemOnly } }));

        var record = new ChangeRecord(EntityRef.ForKey("Order", "system-only-1"), new Dictionary<string, object?>(), new Dictionary<string, object?> { ["x"] = 1 }) {
            TenantId = Guid.NewGuid()
        };

        await tracker.RecordChangeAsync(record, TestContext.Current.CancellationToken);
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await context.Changes.FirstAsync(c => c.ForEntityId == "system-only-1", TestContext.Current.CancellationToken);
        Assert.Null(entity.TenantId);
    }

    [Fact]
    public async Task MultiTenantStrict_ThrowsWhenCallerDoesNotSupplyTenant()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>();
        var tracker = new PostgresChangeTracker(
            factory, Options.Create(new EntityRefOptions()), Options.Create(new PostgresChangeTrackerOptions { Tenancy = new() { Mode = TenancyMode.MultiTenantStrict } }));

        var record = new ChangeRecord(EntityRef.ForKey("Order", "strict-missing"), new Dictionary<string, object?>(), new Dictionary<string, object?> { ["x"] = 1 });
        await Assert.ThrowsAsync<ArgumentNullException>(() => tracker.RecordChangeAsync(record, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SingleTenantDefault_NullTenantUsesFeatureDefault()
    {
        var defaultTenant = Guid.NewGuid();
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>();
        var tracker = new PostgresChangeTracker(
            factory, Options.Create(new EntityRefOptions()),
            Options.Create(new PostgresChangeTrackerOptions { Tenancy = new() { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = defaultTenant } }));

        var record = new ChangeRecord(EntityRef.ForKey("Order", "default-tenant-1"), new Dictionary<string, object?>(), new Dictionary<string, object?> { ["x"] = 1 });
        await tracker.RecordChangeAsync(record, TestContext.Current.CancellationToken);
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await context.Changes.FirstAsync(c => c.ForEntityId == "default-tenant-1", TestContext.Current.CancellationToken);
        Assert.Equal(defaultTenant, entity.TenantId);
    }

    private static string? AsString(object? value)
        => value switch {
            null => null,
            JsonElement element => element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString(),
            var _ => value.ToString()
        };
}