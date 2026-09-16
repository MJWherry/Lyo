using Lyo.Audit.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Lyo.Common.Core.Identifiers;
namespace Lyo.Audit.Postgres.Tests;

public class PostgresAuditRecorderTests : IAsyncDisposable
{
    private readonly AuditPostgresFixture _fixture;

    public PostgresAuditRecorderTests(AuditPostgresFixture fixture) => _fixture = fixture;

    public async ValueTask DisposeAsync()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE audit.audit_changes RESTART IDENTITY CASCADE;
            TRUNCATE TABLE audit.audit_events RESTART IDENTITY CASCADE;
            """, TestCt);
    }

    [Fact]
    public async Task RecordChange_PersistsToDatabase()
    {
        var entity = EntityRef.ForKey("TestApp.Models.Order", "42");
        var actor = EntityRef.ForKey("User", "u-1");
        var change = new AuditChange(
            entity, new Dictionary<string, object?> { ["Name"] = "Old Order", ["Status"] = "Draft" },
            new Dictionary<string, object?> { ["Name"] = "Updated Order", ["Status"] = "Submitted" }) { Actor = actor };

        await _fixture.Recorder.RecordChangeAsync(change, TestCt);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var entities = await context.AuditChanges.ToListAsync(TestCt);
        Assert.Single(entities);
        Assert.NotEqual(Guid.Empty, entities[0].Id);
        Assert.Equal("TestApp.Models.Order", entities[0].SubjectEntityType);
        Assert.Equal("42", entities[0].SubjectEntityId);
        Assert.Equal("User", entities[0].ActorEntityType);
        Assert.Equal("u-1", entities[0].ActorEntityId);
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

        await _fixture.Recorder.RecordEventAsync(evt, TestCt);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var entities = await context.AuditEvents.ToListAsync(TestCt);
        Assert.Single(entities);
        Assert.NotEqual(Guid.Empty, entities[0].Id);
        Assert.Equal("UserLogin", entities[0].EventType);
        Assert.Equal("User signed in successfully", entities[0].Message);
        Assert.Equal("User", entities[0].SubjectEntityType);
        Assert.Equal("user-123", entities[0].SubjectEntityId);
        Assert.Equal("User", entities[0].ActorEntityType);
        Assert.Equal("user-123", entities[0].ActorEntityId);
        Assert.NotNull(entities[0].MetadataJson);
        Assert.Contains("192.168.1.1", entities[0].MetadataJson);
    }

    [Fact]
    public async Task RecordEvent_WithNullMetadata_StoresNullJson()
    {
        var evt = new AuditEvent(EntityRef.ForKey("System", "app"), "SimpleEvent", "No metadata");
        await _fixture.Recorder.RecordEventAsync(evt, TestCt);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var entity = await context.AuditEvents.FirstOrDefaultAsync(e => e.EventType == "SimpleEvent", TestCt);
        Assert.NotNull(entity);
        Assert.Null(entity.MetadataJson);
        Assert.Null(entity.ActorEntityType);
        Assert.Null(entity.ActorEntityId);
    }

    [Fact]
    public async Task RecordChanges_Bulk_PersistsAllToDatabase()
    {
        var changes = new[] {
            new AuditChange(EntityRef.ForKey("App.A", "1"), new Dictionary<string, object?> { ["x"] = 1 }, new Dictionary<string, object?> { ["x"] = 2 }),
            new AuditChange(EntityRef.ForKey("App.B", "2"), new Dictionary<string, object?> { ["y"] = "a" }, new Dictionary<string, object?> { ["y"] = "b" })
        };

        await _fixture.Recorder.RecordChangesAsync(changes, TestCt);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var entities = await context.AuditChanges.ToListAsync(TestCt);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entities, e => e.SubjectEntityType == "App.A" && e.SubjectEntityId == "1");
        Assert.Contains(entities, e => e.SubjectEntityType == "App.B" && e.SubjectEntityId == "2");
    }

    [Fact]
    public async Task RecordEvents_Bulk_PersistsAllToDatabase()
    {
        var events = new[] {
            new AuditEvent(EntityRef.ForKey("E", "1"), "BulkEvent1", "First"),
            new AuditEvent(EntityRef.ForKey("E", "2"), "BulkEvent2", "Second", EntityRef.ForKey("User", "actor-1"))
        };

        await _fixture.Recorder.RecordEventsAsync(events, TestCt);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var entities = await context.AuditEvents.ToListAsync(TestCt);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entities, e => e.EventType == "BulkEvent1");
        Assert.Contains(entities, e => e.EventType == "BulkEvent2" && e.ActorEntityType == "User" && e.ActorEntityId == "actor-1");
    }

    [Fact]
    public async Task RecordEvent_WithTenant_FiltersByTenant()
    {
        var tenantA = LyoGuid.CreateV7();
        var tenantB = LyoGuid.CreateV7();
        var evtA = new AuditEvent(EntityRef.ForKey("Doc", "doc-a"), "Created", "doc A") { TenantId = tenantA };
        var evtB = new AuditEvent(EntityRef.ForKey("Doc", "doc-b"), "Created", "doc B") { TenantId = tenantB };
        var evtSystem = new AuditEvent(EntityRef.ForKey("System", "system-1"), "Created", "system");
        await _fixture.Recorder.RecordEventAsync(evtA, TestCt);
        await _fixture.Recorder.RecordEventAsync(evtB, TestCt);
        await _fixture.Recorder.RecordEventAsync(evtSystem, TestCt);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var onlyTenantA = await context.AuditEvents.WhereTenant(tenantA).ToListAsync(TestCt);
        Assert.Single(onlyTenantA);
        Assert.Equal("doc-a", onlyTenantA[0].SubjectEntityId);
        var onlySystem = await context.AuditEvents.WhereTenant(null).ToListAsync(TestCt);
        Assert.Single(onlySystem);
        Assert.Equal("system-1", onlySystem[0].SubjectEntityId);
        var tenantAOrSystem = await context.AuditEvents.WhereTenantOrSystem(tenantA).ToListAsync(TestCt);
        Assert.Equal(2, tenantAOrSystem.Count);
        Assert.Contains(tenantAOrSystem, e => e.SubjectEntityId == "doc-a");
        Assert.Contains(tenantAOrSystem, e => e.SubjectEntityId == "system-1");
    }

    [Fact]
    public async Task SystemOnly_PersistsNullTenantRegardlessOfCaller()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        var recorder = new PostgresAuditRecorder(
            factory, new EntityRefOptions(), new PostgresAuditOptions { Tenancy = new() { Mode = TenancyMode.SystemOnly } });

        var evt = new AuditEvent(EntityRef.ForKey("Doc", "system-only"), "Created", "ignored tenant") { TenantId = LyoGuid.CreateV7() };
        await recorder.RecordEventAsync(evt, TestCt);
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var entity = await context.AuditEvents.FirstAsync(e => e.SubjectEntityId == "system-only", TestCt);
        Assert.Null(entity.TenantId);
    }

    [Fact]
    public async Task MultiTenantStrict_ThrowsWhenCallerDoesNotSupplyTenant()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        var recorder = new PostgresAuditRecorder(
            factory, new EntityRefOptions(), new PostgresAuditOptions { Tenancy = new() { Mode = TenancyMode.MultiTenantStrict } });

        var evt = new AuditEvent(EntityRef.ForKey("Doc", "strict-missing"), "Created", "no tenant");
        await Assert.ThrowsAsync<ArgumentNullException>(() => recorder.RecordEventAsync(evt, TestCt));
    }

    [Fact]
    public async Task SingleTenantDefault_NullTenantUsesFeatureDefault()
    {
        var defaultTenant = LyoGuid.CreateV7();
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        var recorder = new PostgresAuditRecorder(
            factory, new EntityRefOptions(),
            new PostgresAuditOptions { Tenancy = new() { Mode = TenancyMode.SingleTenantDefault, DefaultTenantId = defaultTenant } });

        var evt = new AuditEvent(EntityRef.ForKey("Doc", "default-tenant"), "Created", "uses default");
        await recorder.RecordEventAsync(evt, TestCt);
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var entity = await context.AuditEvents.FirstAsync(e => e.SubjectEntityId == "default-tenant", TestCt);
        Assert.Equal(defaultTenant, entity.TenantId);
    }

    [Fact]
    public async Task RecordChange_WithTenant_FiltersByTenant()
    {
        var tenantA = LyoGuid.CreateV7();
        var tenantB = LyoGuid.CreateV7();
        var changeA = new AuditChange(
            EntityRef.ForKey("Order", "1"), new Dictionary<string, object?> { ["x"] = 1 }, new Dictionary<string, object?> { ["x"] = 2 }) { TenantId = tenantA };

        var changeB = new AuditChange(
            EntityRef.ForKey("Order", "2"), new Dictionary<string, object?> { ["x"] = 1 }, new Dictionary<string, object?> { ["x"] = 2 }) { TenantId = tenantB };

        await _fixture.Recorder.RecordChangeAsync(changeA, TestCt);
        await _fixture.Recorder.RecordChangeAsync(changeB, TestCt);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestCt);
        var onlyTenantA = await context.AuditChanges.WhereTenant(tenantA).ToListAsync(TestCt);
        Assert.Single(onlyTenantA);
        Assert.Equal("1", onlyTenantA[0].SubjectEntityId);
        var onlyTenantB = await context.AuditChanges.WhereTenant(tenantB).ToListAsync(TestCt);
        Assert.Single(onlyTenantB);
        Assert.Equal("2", onlyTenantB[0].SubjectEntityId);
    }
}