using Lyo.People.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.People.Postgres.Tests;

public class PeoplePostgresExtensionsTests
{
    private readonly PeoplePostgresFixture _fixture;

    public PeoplePostgresExtensionsTests(PeoplePostgresFixture fixture) => _fixture = fixture;

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
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<PeopleDbContext>>();
        await using var context = factory.CreateDbContext();
        var canConnect = await context.Database.CanConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<PeopleDbContext>>();
        await using var context = factory.CreateDbContext();
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.Empty(pending);
    }
}
