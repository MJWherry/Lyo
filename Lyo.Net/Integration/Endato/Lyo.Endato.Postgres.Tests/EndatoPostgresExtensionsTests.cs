using Lyo.Endato.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Endato.Postgres.Tests;

public class EndatoPostgresExtensionsTests
{
    private readonly EndatoPostgresFixture _fixture;

    public EndatoPostgresExtensionsTests(EndatoPostgresFixture fixture) => _fixture = fixture;

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
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<EndatoDbContext>>();
        await using var context = factory.CreateDbContext();
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<EndatoDbContext>>();
        await using var context = factory.CreateDbContext();
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.Empty(pending);
    }
}
