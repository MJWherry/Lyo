using Lyo.Endato.Postgres.Database;
using Lyo.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Lyo.Endato.Postgres.Tests;

public class EndatoPostgresExtensionsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly ITestOutputHelper _output;
    private IServiceProvider? _serviceProvider;

    public EndatoPostgresExtensionsTests(ITestOutputHelper output) => _output = output;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        var connectionString = _container.GetConnectionString();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddEndatoDbContextFactory(new PostgresEndatoOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        _serviceProvider = services.BuildServiceProvider();
        using var scope = _serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EndatoDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public void AddEndatoDbContext_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddEndatoDbContext(null!, "Host=localhost"));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddEndatoDbContext_WithNullConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddEndatoDbContext((string)null!));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddEndatoDbContext_WithEmptyConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddEndatoDbContext(""));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddEndatoDbContextFactory_WithNullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddEndatoDbContextFactory((Action<PostgresEndatoOptions>)null!));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public void AddEndatoDbContextFactory_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddEndatoDbContextFactoryFromConfiguration(null!));
        Assert.Equal("configuration", ex.ParamName);
    }

    [Fact]
    public void AddEndatoDbContextFactory_WithEmptyConfigSectionName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var ex = Assert.Throws<ArgumentException>(() => services.AddEndatoDbContextFactoryFromConfiguration(config, ""));
        Assert.Equal("configSectionName", ex.ParamName);
    }

    [Fact]
    public async Task DbContext_CanConnectAndQuerySchema()
    {
        Assert.NotNull(_serviceProvider);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<EndatoDbContext>>();
        await using var context = factory.CreateDbContext();
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        Assert.NotNull(_serviceProvider);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<EndatoDbContext>>();
        await using var context = factory.CreateDbContext();
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Empty(pending);
    }
}