using Lyo.Audit.Postgres.Database;
using Lyo.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Lyo.Audit.Postgres.Tests;

public class PostgresAuditRecorderTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly ITestOutputHelper _output;
    private IAuditRecorder? _recorder;
    private IServiceProvider? _serviceProvider;

    public PostgresAuditRecorderTests(ITestOutputHelper output) => _output = output;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        var connectionString = _container.GetConnectionString();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddDbContextFactory<AuditDbContext>(opts => opts.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "audit")));
        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope()) {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
            await using var context = await factory.CreateDbContextAsync();
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        var recorderFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        _recorder = new PostgresAuditRecorder(recorderFactory);
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IDisposable d)
            d.Dispose();

        await _container.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task RecordChange_PersistsToDatabase()
    {
        Assert.NotNull(_recorder);
        Assert.NotNull(_serviceProvider);
        var change = new AuditChange(
            "TestApp.Models.Order, TestApp", new Dictionary<string, object?> { ["Name"] = "Old Order", ["Status"] = "Draft" },
            new Dictionary<string, object?> { ["Name"] = "Updated Order", ["Status"] = "Submitted" });

        await _recorder.RecordChangeAsync(change, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        var entities = await context.AuditChanges.ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Single(entities);
        Assert.NotEqual(Guid.Empty, entities[0].Id);
        Assert.Equal("TestApp.Models.Order, TestApp", entities[0].TypeAssemblyFullName);
        Assert.Contains("Old Order", entities[0].OldValuesJson);
        Assert.Contains("Updated Order", entities[0].ChangedPropertiesJson);
    }

    [Fact]
    public async Task RecordEvent_PersistsToDatabase()
    {
        Assert.NotNull(_recorder);
        Assert.NotNull(_serviceProvider);
        var evt = new AuditEvent(
            "UserLogin", "User signed in successfully", "user-123", new Dictionary<string, object?> { ["IpAddress"] = "192.168.1.1", ["UserAgent"] = "TestBot/1.0" }) {
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };

        await _recorder.RecordEventAsync(evt, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        var entities = await context.AuditEvents.ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
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
        Assert.NotNull(_recorder);
        Assert.NotNull(_serviceProvider);
        var evt = new AuditEvent("SimpleEvent", "No metadata");
        await _recorder.RecordEventAsync(evt, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        var entity = await context.AuditEvents.FirstOrDefaultAsync(e => e.EventType == "SimpleEvent", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(entity);
        Assert.Null(entity.MetadataJson);
    }

    [Fact]
    public async Task RecordChanges_Bulk_PersistsAllToDatabase()
    {
        Assert.NotNull(_recorder);
        Assert.NotNull(_serviceProvider);
        var changes = new[] {
            new AuditChange("App.A", new Dictionary<string, object?> { ["x"] = 1 }, new Dictionary<string, object?> { ["x"] = 2 }),
            new AuditChange("App.B", new Dictionary<string, object?> { ["y"] = "a" }, new Dictionary<string, object?> { ["y"] = "b" })
        };

        await _recorder.RecordChangesAsync(changes, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        var entities = await context.AuditChanges.ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entities, e => e.TypeAssemblyFullName == "App.A");
        Assert.Contains(entities, e => e.TypeAssemblyFullName == "App.B");
    }

    [Fact]
    public async Task RecordEvents_Bulk_PersistsAllToDatabase()
    {
        Assert.NotNull(_recorder);
        Assert.NotNull(_serviceProvider);
        var events = new[] { new AuditEvent("BulkEvent1", "First"), new AuditEvent("BulkEvent2", "Second", "actor-1") };
        await _recorder.RecordEventsAsync(events, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<AuditDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        var entities = await context.AuditEvents.ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entities, e => e.EventType == "BulkEvent1");
        Assert.Contains(entities, e => e.EventType == "BulkEvent2");
    }
}