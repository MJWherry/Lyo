using Lyo.People.Postgres.Database;
using Lyo.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Lyo.People.Postgres.Tests;

public class PeoplePostgresExtensionsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly ITestOutputHelper _output;
    private IServiceProvider? _serviceProvider;

    public PeoplePostgresExtensionsTests(ITestOutputHelper output) => _output = output;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        var connectionString = _container.GetConnectionString();
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddProvider(new XunitLoggerProvider(_output));
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope()) {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PeopleDbContext>>();
            await using var context = factory.CreateDbContext();
            await context.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);

    [Fact]
    public void AddPeopleDbContext_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddPeopleDbContext(null!, "Host=localhost"));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddPeopleDbContext_WithNullConnectionString_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPeopleDbContext((string)null!));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddPeopleDbContext_WithEmptyConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddPeopleDbContext(""));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddPeopleDbContext_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddPeopleDbContext("   "));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddPeopleDbContextFactory_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddPeopleDbContextFactory(null!, _ => { }));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddPeopleDbContextFactory_WithNullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPeopleDbContextFactory((Action<PostgresPeopleOptions>)null!));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public void AddPeopleDbContextFactory_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPeopleDbContextFactoryFromConfiguration(null!));
        Assert.Equal("configuration", ex.ParamName);
    }

    [Fact]
    public void AddPeopleDbContextFactory_WithEmptyConfigSectionName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var ex = Assert.Throws<ArgumentException>(() => services.AddPeopleDbContextFactoryFromConfiguration(config, ""));
        Assert.Equal("configSectionName", ex.ParamName);
    }

    [Fact]
    public async Task DbContext_CanConnectAndQuerySchema()
    {
        Assert.NotNull(_serviceProvider);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<PeopleDbContext>>();
        await using var context = factory.CreateDbContext();
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        Assert.NotNull(_serviceProvider);
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<PeopleDbContext>>();
        await using var context = factory.CreateDbContext();
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Empty(pending);
    }
}