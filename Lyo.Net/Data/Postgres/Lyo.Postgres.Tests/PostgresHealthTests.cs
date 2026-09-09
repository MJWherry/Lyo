using Microsoft.EntityFrameworkCore;

namespace Lyo.Postgres.Tests;

public class PostgresHealthTests
{
    // Port 1 is never a Postgres listener; a one-second timeout keeps the unreachable case fast.
    private const string UnreachableConnectionString = "Host=127.0.0.1;Port=1;Database=lyo;Username=lyo;Password=lyo;Timeout=1;Command Timeout=1";

    [Fact]
    public void SchemaMetadataKey_IsStable() => Assert.Equal("database", PostgresHealth.SchemaMetadataKey);

    [Fact]
    public async Task CheckAsync_ReportsUnhealthyWhenTheDatabaseIsUnreachable()
    {
        await using var context = PostgresSchema.CreateContext<TestDbContext>(UnreachableConnectionString, TestPostgresOptions.Schema);
        var result = await PostgresHealth.CheckAsync(context, TestPostgresOptions.Schema, TestContext.Current.CancellationToken);
        Assert.False(result.IsHealthy);
    }

    [Fact]
    public async Task CheckAsync_ReportsUnhealthyWhenTheFactoryCannotConnect()
    {
        var factory = new TestDbContextFactory(UnreachableConnectionString);
        var result = await PostgresHealth.CheckAsync(factory, TestPostgresOptions.Schema, TestContext.Current.CancellationToken);
        Assert.False(result.IsHealthy);
    }

    [Fact]
    public async Task CheckAsync_SurfacesAFactoryFailureAsTheUnhealthyReason()
    {
        var result = await PostgresHealth.CheckAsync(new ThrowingDbContextFactory(), TestPostgresOptions.Schema, TestContext.Current.CancellationToken);
        Assert.False(result.IsHealthy);
        Assert.Equal("No database configured for this store.", result.Message);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task CheckAsync_ThrowsWhenTheFactoryIsNull()
        => await Assert.ThrowsAsync<ArgumentNullException>(
            () => PostgresHealth.CheckAsync<TestDbContext>(null!, TestPostgresOptions.Schema, TestContext.Current.CancellationToken));

    private sealed class TestDbContextFactory(string connectionString) : IDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext() => PostgresSchema.CreateContext<TestDbContext>(connectionString, TestPostgresOptions.Schema);
    }

    private sealed class ThrowingDbContextFactory : IDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext() => throw new InvalidOperationException("No database configured for this store.");
    }
}
