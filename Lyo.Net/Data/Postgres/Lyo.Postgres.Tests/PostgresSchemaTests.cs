using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Lyo.Postgres.Tests;

public class PostgresSchemaTests
{
    private const string ConnectionString = "Host=localhost;Database=lyo_tests;Username=lyo;Password=lyo";

    [Fact]
    public void MigrationsHistoryTable_MatchesTheEfCoreDefaultName() => Assert.Equal("__EFMigrationsHistory", PostgresSchema.MigrationsHistoryTable);

    [Fact]
    public void BuildOptions_PinsHistoryTableToTheGivenSchema()
    {
        var options = PostgresSchema.BuildOptions<TestDbContext>(ConnectionString, TestPostgresOptions.Schema);
        var relational = RelationalOptionsExtension.Extract(options);
        Assert.Equal(ConnectionString, relational.ConnectionString);
        Assert.Equal(PostgresSchema.MigrationsHistoryTable, relational.MigrationsHistoryTableName);
        Assert.Equal(TestPostgresOptions.Schema, relational.MigrationsHistoryTableSchema);
    }

    [Theory]
    [InlineData("", TestPostgresOptions.Schema)]
    [InlineData(ConnectionString, "")]
    [InlineData(ConnectionString, "  ")]
    public void BuildOptions_ThrowsOnMissingArguments(string connectionString, string schema)
        => Assert.Throws<ArgumentException>(() => PostgresSchema.BuildOptions<TestDbContext>(connectionString, schema));

    [Fact]
    public void CreateContext_ReturnsAContextConfiguredForTheSchema()
    {
        using var context = PostgresSchema.CreateContext<TestDbContext>(ConnectionString, TestPostgresOptions.Schema);
        var relational = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>());
        Assert.Equal(TestPostgresOptions.Schema, relational.MigrationsHistoryTableSchema);
    }

    [Fact]
    public async Task EnsureAsync_ThrowsWhenTheSchemaIsBlank()
    {
        await using var context = PostgresSchema.CreateContext<TestDbContext>(ConnectionString, TestPostgresOptions.Schema);
        await Assert.ThrowsAsync<ArgumentException>(() => PostgresSchema.EnsureAsync(context, " ", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EnsureAsync_ThrowsWhenTheContextIsNull()
        => await Assert.ThrowsAsync<ArgumentNullException>(() => PostgresSchema.EnsureAsync(null!, TestPostgresOptions.Schema, TestContext.Current.CancellationToken));
}
