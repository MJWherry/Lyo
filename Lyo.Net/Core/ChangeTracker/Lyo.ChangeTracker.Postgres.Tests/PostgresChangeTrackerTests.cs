using System.Text.Json;
using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Common;
using Lyo.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Lyo.ChangeTracker.Postgres.Tests;

public class PostgresChangeTrackerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly ITestOutputHelper _output;
    private IChangeTracker? _changeTracker;
    private IServiceProvider? _serviceProvider;

    public PostgresChangeTrackerTests(ITestOutputHelper output) => _output = output;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        var connectionString = _container.GetConnectionString();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddDbContextFactory<ChangeTrackerDbContext>(opts => opts.UseNpgsql(
            connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresChangeTrackerOptions.Schema)));

        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope()) {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>();
            await using var context = await factory.CreateDbContextAsync();
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        var trackerFactory = _serviceProvider.GetRequiredService<IDbContextFactory<ChangeTrackerDbContext>>();
        _changeTracker = new PostgresChangeTracker(trackerFactory);
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IDisposable d)
            d.Dispose();

        await _container.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task RecordChangeAsync_PersistsAndQueriesByEntity()
    {
        Assert.NotNull(_changeTracker);
        var forEntity = EntityRef.ForKey("Order", "123");
        var fromEntity = EntityRef.ForKey("User", "42");
        var change = new ChangeRecord(forEntity, new Dictionary<string, object?> { ["Status"] = "Draft" }, new Dictionary<string, object?> { ["Status"] = "Submitted" }) {
            FromEntity = fromEntity, ChangeType = "Updated", Message = "Order submitted"
        };

        await _changeTracker.RecordChangeAsync(change, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var byId = await _changeTracker.GetByIdAsync(change.Id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var history = await _changeTracker.GetForEntityAsync(forEntity, TestContext.Current.CancellationToken).ConfigureAwait(false);
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
        Assert.NotNull(_changeTracker);
        var older = new ChangeRecord(EntityRef.ForKey("Order", "A"), new Dictionary<string, object?>(), new Dictionary<string, object?> { ["Status"] = "Draft" }) {
            Timestamp = DateTime.UtcNow.AddMinutes(-10), ChangeType = "Created"
        };

        var newer = new ChangeRecord(
            EntityRef.ForKey("Order", "B"), new Dictionary<string, object?> { ["Status"] = "Draft" }, new Dictionary<string, object?> { ["Status"] = "Submitted" }) {
            Timestamp = DateTime.UtcNow, ChangeType = "Updated"
        };

        await _changeTracker.RecordChangesAsync([older, newer], TestContext.Current.CancellationToken).ConfigureAwait(false);
        var history = await _changeTracker.GetForEntityTypeAsync("Order", ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(history.Count >= 2);
        Assert.Equal(newer.Id, history[0].Id);
        Assert.Equal(older.Id, history[1].Id);
    }

    [Fact]
    public async Task DeleteForEntityAsync_RemovesTrackedHistory()
    {
        Assert.NotNull(_changeTracker);
        var forEntity = EntityRef.ForKey("Invoice", "9001");
        var change = new ChangeRecord(forEntity, new Dictionary<string, object?>(), new Dictionary<string, object?> { ["Status"] = "Paid" });
        await _changeTracker.RecordChangeAsync(change, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await _changeTracker.DeleteForEntityAsync(forEntity, TestContext.Current.CancellationToken).ConfigureAwait(false);
        var history = await _changeTracker.GetForEntityAsync(forEntity, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Empty(history);
    }

    private static string? AsString(object? value)
        => value switch {
            null => null,
            JsonElement element => element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString(),
            var _ => value.ToString()
        };
}