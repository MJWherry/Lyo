using Lyo.Audit.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Audit.Postgres.Tests;

public class PostgresAuditRecorderTests : IAsyncDisposable
{
    private readonly AuditPostgresFixture _fixture;

    public PostgresAuditRecorderTests(AuditPostgresFixture fixture) => _fixture = fixture;

    public async ValueTask DisposeAsync()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE audit.audit_changes RESTART IDENTITY CASCADE;
            TRUNCATE TABLE audit.audit_events RESTART IDENTITY CASCADE;
            """, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RecordChange_PersistsToDatabase()
    {
        var entity = EntityRef.ForKey("TestApp.Models.Order", "42");
        var actor = EntityRef.ForKey("User", "u-1");
        var change = new AuditChange(
            entity, new Dictionary<string, object?> { ["Name"] = "Old Order", ["Status"] = "Draft" },
            new Dictionary<string, object?> { ["Name"] = "Updated Order", ["Status"] = "Submitted" }) { Actor = actor };

        await _fixture.Recorder.RecordChangeAsync(change, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entities = await context.AuditChanges.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(entities);
        Assert.NotEqual(Guid.Empty, entities[0].Id);
        Assert.Equal("TestApp.Models.Order", entities[0].ForEntityType);
        Assert.Equal("42", entities[0].ForEntityId);
        Assert.Equal("User", entities[0].FromEntityType);
        Assert.Equal("u-1", entities[0].FromEntityId);
        Assert.Contains("Old Order", entities[0].OldValuesJson);
        Assert.Contains("Updated Order", entities[0].ChangedPropertiesJson);
    }

    [Fact]
    public async Task RecordEvent_PersistsToDatabase()
    {
        var subject = EntityRef.ForKey("User", "user-123");
        var actor = EntityRef.ForKey("User", "user-123");
        var evt = new AuditEvent(
            subject, "UserLogin", "User signed in successfully", actor, new Dictionary<string, object?> { ["IpAddress"] = "192.168.1.1", ["UserAgent"] = "TestBot/1.0" }) {
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };

        await _fixture.Recorder.RecordEventAsync(evt, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entities = await context.AuditEvents.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(entities);
        Assert.NotEqual(Guid.Empty, entities[0].Id);
        Assert.Equal("UserLogin", entities[0].EventType);
        Assert.Equal("User signed in successfully", entities[0].Message);
        Assert.Equal("User", entities[0].ForEntityType);
        Assert.Equal("user-123", entities[0].ForEntityId);
        Assert.Equal("User", entities[0].FromEntityType);
        Assert.Equal("user-123", entities[0].FromEntityId);
        Assert.NotNull(entities[0].MetadataJson);
        Assert.Contains("192.168.1.1", entities[0].MetadataJson);
    }

    [Fact]
    public async Task RecordEvent_WithNullMetadata_StoresNullJson()
    {
        var evt = new AuditEvent(EntityRef.ForKey("System", "app"), "SimpleEvent", "No metadata");
        await _fixture.Recorder.RecordEventAsync(evt, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await context.AuditEvents.FirstOrDefaultAsync(e => e.EventType == "SimpleEvent", TestContext.Current.CancellationToken);
        Assert.NotNull(entity);
        Assert.Null(entity.MetadataJson);
        Assert.Null(entity.FromEntityType);
        Assert.Null(entity.FromEntityId);
    }

    [Fact]
    public async Task RecordChanges_Bulk_PersistsAllToDatabase()
    {
        var changes = new[] {
            new AuditChange(EntityRef.ForKey("App.A", "1"), new Dictionary<string, object?> { ["x"] = 1 }, new Dictionary<string, object?> { ["x"] = 2 }),
            new AuditChange(EntityRef.ForKey("App.B", "2"), new Dictionary<string, object?> { ["y"] = "a" }, new Dictionary<string, object?> { ["y"] = "b" })
        };

        await _fixture.Recorder.RecordChangesAsync(changes, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entities = await context.AuditChanges.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entities, e => e.ForEntityType == "App.A" && e.ForEntityId == "1");
        Assert.Contains(entities, e => e.ForEntityType == "App.B" && e.ForEntityId == "2");
    }

    [Fact]
    public async Task RecordEvents_Bulk_PersistsAllToDatabase()
    {
        var events = new[] {
            new AuditEvent(EntityRef.ForKey("E", "1"), "BulkEvent1", "First"),
            new AuditEvent(EntityRef.ForKey("E", "2"), "BulkEvent2", "Second", EntityRef.ForKey("User", "actor-1"))
        };

        await _fixture.Recorder.RecordEventsAsync(events, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entities = await context.AuditEvents.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entities, e => e.EventType == "BulkEvent1");
        Assert.Contains(entities, e => e.EventType == "BulkEvent2" && e.FromEntityType == "User" && e.FromEntityId == "actor-1");
    }

    [Fact]
    public async Task RecordEvent_WithTenant_FiltersByTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var evtA = new AuditEvent(EntityRef.ForKey("Doc", "doc-a"), "Created", "doc A") { TenantId = tenantA };
        var evtB = new AuditEvent(EntityRef.ForKey("Doc", "doc-b"), "Created", "doc B") { TenantId = tenantB };
        var evtSystem = new AuditEvent(EntityRef.ForKey("System", "system-1"), "Created", "system");
        await _fixture.Recorder.RecordEventAsync(evtA, TestContext.Current.CancellationToken);
        await _fixture.Recorder.RecordEventAsync(evtB, TestContext.Current.CancellationToken);
        await _fixture.Recorder.RecordEventAsync(evtSystem, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var onlyTenantA = await context.AuditEvents.WhereTenant(tenantA).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(onlyTenantA);
        Assert.Equal("doc-a", onlyTenantA[0].ForEntityId);
        var onlySystem = await context.AuditEvents.WhereTenant(null).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(onlySystem);
        Assert.Equal("system-1", onlySystem[0].ForEntityId);
        var tenantAOrSystem = await context.AuditEvents.WhereTenantOrSystem(tenantA).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, tenantAOrSystem.Count);
        Assert.Contains(tenantAOrSystem, e => e.ForEntityId == "doc-a");
        Assert.Contains(tenantAOrSystem, e => e.ForEntityId == "system-1");
    }

    [Fact]
    public async Task SystemOnly_PersistsNullTenantRegardlessOfCaller()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        var recorder = new PostgresAuditRecorder(
            factory, Options.Create(new EntityRefOptions()), Options.Create(new PostgresAuditOptions { Tenancy = new() { Mode = TenancyMode.SystemOnly } }));

        var evt = new AuditEvent(EntityRef.ForKey("Doc", "system-only"), "Created", "ignored tenant") { TenantId = Guid.NewGuid() };
        await recorder.RecordEventAsync(evt, TestContext.Current.CancellationToken);
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await context.AuditEvents.FirstAsync(e => e.ForEntityId == "system-only", TestContext.Current.CancellationToken);
        Assert.Null(entity.TenantId);
    }

    [Fact]
    public async Task MultiTenantStrict_ThrowsWhenCallerDoesNotSupplyTenant()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        var recorder = new PostgresAuditRecorder(
            factory, Options.Create(new EntityRefOptions()), Options.Create(new PostgresAuditOptions { Tenancy = new() { Mode = TenancyMode.MultiTenantStrict } }));

        var evt = new AuditEvent(EntityRef.ForKey("Doc", "strict-missing"), "Created", "no tenant");
        await Assert.ThrowsAsync<ArgumentNullException>(() => recorder.RecordEventAsync(evt, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SingleTenantDefault_NullTenantUsesFeatureDefault()
    {
        var defaultTenant = Guid.NewGuid();
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        var recorder = new PostgresAuditRecorder(
            factory, Options.Create(new EntityRefOptions()),
            Options.Create(new PostgresAuditOptions { Tenancy = new() { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = defaultTenant } }));

        var evt = new AuditEvent(EntityRef.ForKey("Doc", "default-tenant"), "Created", "uses default");
        await recorder.RecordEventAsync(evt, TestContext.Current.CancellationToken);
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await context.AuditEvents.FirstAsync(e => e.ForEntityId == "default-tenant", TestContext.Current.CancellationToken);
        Assert.Equal(defaultTenant, entity.TenantId);
    }

    [Fact]
    public async Task RecordChange_WithTenant_FiltersByTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var changeA = new AuditChange(
            EntityRef.ForKey("Order", "1"), new Dictionary<string, object?> { ["x"] = 1 }, new Dictionary<string, object?> { ["x"] = 2 }) { TenantId = tenantA };

        var changeB = new AuditChange(
            EntityRef.ForKey("Order", "2"), new Dictionary<string, object?> { ["x"] = 1 }, new Dictionary<string, object?> { ["x"] = 2 }) { TenantId = tenantB };

        await _fixture.Recorder.RecordChangeAsync(changeA, TestContext.Current.CancellationToken);
        await _fixture.Recorder.RecordChangeAsync(changeB, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var onlyTenantA = await context.AuditChanges.WhereTenant(tenantA).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(onlyTenantA);
        Assert.Equal("1", onlyTenantA[0].ForEntityId);
        var onlyTenantB = await context.AuditChanges.WhereTenant(tenantB).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(onlyTenantB);
        Assert.Equal("2", onlyTenantB[0].ForEntityId);
    }
}