using Lyo.Audit.Postgres.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Audit.Postgres.Tests;

public class ExtensionsTests
{
    [Fact]
    public void AddAuditDbContext_WithConnectionString_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.AddAuditDbContext("Host=localhost;Database=test;Username=u;Password=p");
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var auditDbContext = scope.ServiceProvider.GetService<AuditDbContext>();
        Assert.NotNull(auditDbContext);
    }

    [Fact]
    public void AddAuditDbContext_WithNullServices_Throws() => Assert.Throws<ArgumentNullException>(() => Extensions.AddAuditDbContext(null!, "conn"));

    [Fact]
    public void AddAuditDbContext_WithNullOrEmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddAuditDbContext((string)null!));
        Assert.Throws<ArgumentException>(() => services.AddAuditDbContext(""));
    }

    [Fact]
    public void AddPostgresAuditRecorder_WithNullOptions_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddPostgresAuditRecorder((PostgresAuditOptions)null!));
    }

    [Fact]
    public void AddPostgresAuditRecorder_WithEmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();
        var options = new PostgresAuditOptions { ConnectionString = "" };
        Assert.Throws<ArgumentException>(() => services.AddPostgresAuditRecorder(options));
    }
}