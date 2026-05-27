using Lyo.Audit.Postgres.Database;
using Lyo.EntityReference.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
                new Dictionary<string, object?> { ["Name"] = "Updated Order", ["Status"] = "Submitted" })
            { Actor = actor };

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
            subject, "UserLogin", "User signed in successfully", actor,
            new Dictionary<string, object?> { ["IpAddress"] = "192.168.1.1", ["UserAgent"] = "TestBot/1.0" }) {
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
}
