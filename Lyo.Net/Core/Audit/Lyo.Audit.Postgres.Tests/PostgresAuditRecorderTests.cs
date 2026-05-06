using Lyo.Audit.Postgres.Database;
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
            """,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RecordChange_PersistsToDatabase()
    {
        var change = new AuditChange(
            "TestApp.Models.Order, TestApp", new Dictionary<string, object?> { ["Name"] = "Old Order", ["Status"] = "Draft" },
            new Dictionary<string, object?> { ["Name"] = "Updated Order", ["Status"] = "Submitted" });

        await _fixture.Recorder.RecordChangeAsync(change, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entities = await context.AuditChanges.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(entities);
        Assert.NotEqual(Guid.Empty, entities[0].Id);
        Assert.Equal("TestApp.Models.Order, TestApp", entities[0].TypeAssemblyFullName);
        Assert.Contains("Old Order", entities[0].OldValuesJson);
        Assert.Contains("Updated Order", entities[0].ChangedPropertiesJson);
    }

    [Fact]
    public async Task RecordEvent_PersistsToDatabase()
    {
        var evt = new AuditEvent(
            "UserLogin", "User signed in successfully", "user-123", new Dictionary<string, object?> { ["IpAddress"] = "192.168.1.1", ["UserAgent"] = "TestBot/1.0" }) {
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
        Assert.Equal("user-123", entities[0].Actor);
        Assert.NotNull(entities[0].MetadataJson);
        Assert.Contains("192.168.1.1", entities[0].MetadataJson);
    }

    [Fact]
    public async Task RecordEvent_WithNullMetadata_StoresNullJson()
    {
        var evt = new AuditEvent("SimpleEvent", "No metadata");
        await _fixture.Recorder.RecordEventAsync(evt, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entity = await context.AuditEvents.FirstOrDefaultAsync(e => e.EventType == "SimpleEvent", TestContext.Current.CancellationToken);
        Assert.NotNull(entity);
        Assert.Null(entity.MetadataJson);
    }

    [Fact]
    public async Task RecordChanges_Bulk_PersistsAllToDatabase()
    {
        var changes = new[] {
            new AuditChange("App.A", new Dictionary<string, object?> { ["x"] = 1 }, new Dictionary<string, object?> { ["x"] = 2 }),
            new AuditChange("App.B", new Dictionary<string, object?> { ["y"] = "a" }, new Dictionary<string, object?> { ["y"] = "b" })
        };

        await _fixture.Recorder.RecordChangesAsync(changes, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entities = await context.AuditChanges.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entities, e => e.TypeAssemblyFullName == "App.A");
        Assert.Contains(entities, e => e.TypeAssemblyFullName == "App.B");
    }

    [Fact]
    public async Task RecordEvents_Bulk_PersistsAllToDatabase()
    {
        var events = new[] { new AuditEvent("BulkEvent1", "First"), new AuditEvent("BulkEvent2", "Second", "actor-1") };
        await _fixture.Recorder.RecordEventsAsync(events, TestContext.Current.CancellationToken);
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var entities = await context.AuditEvents.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entities, e => e.EventType == "BulkEvent1");
        Assert.Contains(entities, e => e.EventType == "BulkEvent2");
    }
}
