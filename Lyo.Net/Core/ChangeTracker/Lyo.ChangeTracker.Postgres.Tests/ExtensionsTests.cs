using Lyo.ChangeTracker.Postgres.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.ChangeTracker.Postgres.Tests;

public class ExtensionsTests
{
    [Fact]
    public void AddChangeTrackerDbContext_WithConnectionString_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.AddChangeTrackerDbContext("Host=localhost;Database=test;Username=u;Password=p");
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ChangeTrackerDbContext>();
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void AddChangeTrackerDbContext_WithNullServices_Throws() => Assert.Throws<ArgumentNullException>(() => Extensions.AddChangeTrackerDbContext(null!, "conn"));

    [Fact]
    public void AddChangeTrackerDbContext_WithNullOrEmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddChangeTrackerDbContext((string)null!));
        Assert.Throws<ArgumentException>(() => services.AddChangeTrackerDbContext(""));
    }

    [Fact]
    public void AddPostgresChangeTracker_WithNullOptions_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddPostgresChangeTracker((PostgresChangeTrackerOptions)null!));
    }

    [Fact]
    public void AddPostgresChangeTracker_WithEmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();
        var options = new PostgresChangeTrackerOptions { ConnectionString = "" };
        Assert.Throws<ArgumentException>(() => services.AddPostgresChangeTracker(options));
    }
}