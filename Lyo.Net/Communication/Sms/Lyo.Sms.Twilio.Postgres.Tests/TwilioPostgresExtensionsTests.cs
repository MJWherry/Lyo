using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Lyo.Sms.Twilio.Postgres.Tests;

public class TwilioPostgresExtensionsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly ITestOutputHelper _output;
    private IServiceProvider? _serviceProvider;

    public TwilioPostgresExtensionsTests(ITestOutputHelper output) => _output = output;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        var connectionString = _container.GetConnectionString();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddTwilioSmsDbContextFactory(new PostgresTwilioSmsOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        _serviceProvider = services.BuildServiceProvider();
        using var scope = _serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TwilioSmsDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public void AddTwilioSmsDbContext_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddTwilioSmsDbContext(null!, "Host=localhost"));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddTwilioSmsDbContext_WithNullConnectionString_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddTwilioSmsDbContext((string)null!));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddTwilioSmsDbContext_WithEmptyConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddTwilioSmsDbContext(""));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddTwilioSmsDbContextFactory_WithNullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddTwilioSmsDbContextFactory((Action<PostgresTwilioSmsOptions>)null!));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public void AddTwilioSmsDbContextFactory_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddTwilioSmsDbContextFactoryFromConfiguration(null!));
        Assert.Equal("configuration", ex.ParamName);
    }

    [Fact]
    public async Task DbContext_CanConnectAndQuerySchema()
    {
        Assert.NotNull(_serviceProvider);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<TwilioSmsDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        Assert.NotNull(_serviceProvider);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<TwilioSmsDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task DbContext_CanInsertAndRetrieveTwilioSmsLog()
    {
        Assert.NotNull(_serviceProvider);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<TwilioSmsDbContext>>();
        var entity = new TwilioSmsLogEntity {
            Id = "SM1234567890abcdef1234567890abcdef",
            To = "+15551234567",
            From = "+15559876543",
            Body = "Test message",
            IsSuccess = true,
            ElapsedTimeMs = 150,
            CreatedTimestamp = DateTime.UtcNow
        };

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false)) {
            context.TwilioSmsLogs.Add(entity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false)) {
            var retrieved = await context.TwilioSmsLogs.FindAsync(["SM1234567890abcdef1234567890abcdef"], TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.NotNull(retrieved);
            Assert.Equal("+15551234567", retrieved.To);
            Assert.Equal("Test message", retrieved.Body);
            Assert.True(retrieved.IsSuccess);
        }
    }

    [Fact]
    public async Task DbContext_CanStoreFailedSmsLog()
    {
        Assert.NotNull(_serviceProvider);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<TwilioSmsDbContext>>();
        var entity = new TwilioSmsLogEntity {
            Id = "SMfailed1234567890abcdef12345678",
            To = "+15559999999",
            IsSuccess = false,
            ErrorMessage = "Invalid phone number",
            ErrorCode = 21211,
            ElapsedTimeMs = 50,
            CreatedTimestamp = DateTime.UtcNow
        };

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false)) {
            context.TwilioSmsLogs.Add(entity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken).ConfigureAwait(false)) {
            var retrieved = await context.TwilioSmsLogs.FindAsync(["SMfailed1234567890abcdef12345678"], TestContext.Current.CancellationToken).ConfigureAwait(false);
            Assert.NotNull(retrieved);
            Assert.False(retrieved.IsSuccess);
            Assert.Equal("Invalid phone number", retrieved.ErrorMessage);
            Assert.Equal(21211, retrieved.ErrorCode);
        }
    }
}