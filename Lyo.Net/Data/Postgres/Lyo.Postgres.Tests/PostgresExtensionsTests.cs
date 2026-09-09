using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lyo.Postgres.Tests;

public class PostgresExtensionsTests
{
    private static TestPostgresOptions ValidOptions => new() { ConnectionString = "Host=localhost;Database=lyo_tests", EnableAutoMigrations = true };

    [Fact]
    public void AddPostgresDbContextFactory_RegistersOptionsTheFactoryAndTheMigrationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPostgresDbContextFactory<TestDbContext, TestPostgresOptions>(ValidOptions);

        using var provider = services.BuildServiceProvider();
        Assert.Equal(ValidOptions.ConnectionString, provider.GetRequiredService<IOptions<TestPostgresOptions>>().Value.ConnectionString);
        Assert.NotNull(provider.GetRequiredService<IDbContextFactory<TestDbContext>>());
        Assert.Contains(provider.GetServices<IHostedService>(), s => s is PostgresMigrationHostedService<TestDbContext, TestPostgresOptions>);
    }

    [Fact]
    public void AddPostgresDbContextFactory_ScopesMigrationHistoryToThePackageSchema()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPostgresDbContextFactory<TestDbContext, TestPostgresOptions>(ValidOptions);

        using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<IDbContextFactory<TestDbContext>>().CreateDbContext();
        var relational = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>());
        Assert.Equal(PostgresSchema.MigrationsHistoryTable, relational.MigrationsHistoryTableName);
        Assert.Equal(TestPostgresOptions.Schema, relational.MigrationsHistoryTableSchema);
    }

    [Fact]
    public void AddPostgresDbContextFactory_ValidatesBeforeRegisteringAnything()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => services.AddPostgresDbContextFactory<TestDbContext, TestPostgresOptions>(new()));
        Assert.Empty(services);
    }

    [Fact]
    public void AddPostgresDbContextFactory_ThrowsWhenOptionsAreNull()
        => Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddPostgresDbContextFactory<TestDbContext, TestPostgresOptions>(null!));

    [Fact]
    public void AddPostgresMigrations_RegistersTheHostedService()
    {
        var services = new ServiceCollection();
        services.AddPostgresMigrations<TestDbContext, TestPostgresOptions>();
        Assert.Single(services, d => d.ServiceType == typeof(IHostedService));
    }
}
