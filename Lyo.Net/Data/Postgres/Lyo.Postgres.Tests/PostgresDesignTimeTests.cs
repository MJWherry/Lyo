using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Lyo.Postgres.Tests;

/// <summary>
/// These tests mutate process environment variables, so they share a collection to keep xUnit from running them in parallel.
/// </summary>
[Collection(nameof(PostgresDesignTimeTests))]
[CollectionDefinition(nameof(PostgresDesignTimeTests), DisableParallelization = true)]
public class PostgresDesignTimeTests : IDisposable
{
    private const string Primary = "LYO_POSTGRES_TESTS_PRIMARY";
    private const string Secondary = "LYO_POSTGRES_TESTS_SECONDARY";
    private const string PrimaryValue = "Host=primary;Database=lyo";
    private const string SecondaryValue = "Host=secondary;Database=lyo";

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Primary, null);
        Environment.SetEnvironmentVariable(Secondary, null);
    }

    [Fact]
    public void CreateOptions_ReadsTheConnectionStringFromTheEnvironment()
    {
        Environment.SetEnvironmentVariable(Primary, PrimaryValue);
        var options = PostgresDesignTime.CreateOptions<TestDbContext>(Primary, TestPostgresOptions.Schema);
        var relational = RelationalOptionsExtension.Extract(options);
        Assert.Equal(PrimaryValue, relational.ConnectionString);
        Assert.Equal(TestPostgresOptions.Schema, relational.MigrationsHistoryTableSchema);
    }

    [Fact]
    public void CreateOptions_FallsBackWhenTheVariableIsUnset()
    {
        var options = PostgresDesignTime.CreateOptions<TestDbContext>(Primary, TestPostgresOptions.Schema, SecondaryValue);
        Assert.Equal(SecondaryValue, RelationalOptionsExtension.Extract(options).ConnectionString);
    }

    [Fact]
    public void CreateOptions_PrefersTheEnvironmentOverTheFallback()
    {
        Environment.SetEnvironmentVariable(Primary, PrimaryValue);
        var options = PostgresDesignTime.CreateOptions<TestDbContext>(Primary, TestPostgresOptions.Schema, SecondaryValue);
        Assert.Equal(PrimaryValue, RelationalOptionsExtension.Extract(options).ConnectionString);
    }

    [Fact]
    public void CreateOptions_ThrowsWithAnActionableMessageWhenNothingIsSet()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PostgresDesignTime.CreateOptions<TestDbContext>(Primary, TestPostgresOptions.Schema));
        Assert.Contains(Primary, ex.Message);
    }

    [Fact]
    public void CreateOptions_TakesTheFirstVariableThatIsSet()
    {
        Environment.SetEnvironmentVariable(Secondary, SecondaryValue);
        var options = PostgresDesignTime.CreateOptions<TestDbContext>([Primary, Secondary], TestPostgresOptions.Schema);
        Assert.Equal(SecondaryValue, RelationalOptionsExtension.Extract(options).ConnectionString);
    }

    [Fact]
    public void CreateOptions_HonoursPrecedenceAcrossVariables()
    {
        Environment.SetEnvironmentVariable(Primary, PrimaryValue);
        Environment.SetEnvironmentVariable(Secondary, SecondaryValue);
        var options = PostgresDesignTime.CreateOptions<TestDbContext>([Primary, Secondary], TestPostgresOptions.Schema);
        Assert.Equal(PrimaryValue, RelationalOptionsExtension.Extract(options).ConnectionString);
    }

    [Fact]
    public void CreateOptions_NamesEveryCandidateVariableWhenNoneIsSet()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PostgresDesignTime.CreateOptions<TestDbContext>([Primary, Secondary], TestPostgresOptions.Schema));
        Assert.Contains(Primary, ex.Message);
        Assert.Contains(Secondary, ex.Message);
    }

    [Fact]
    public void CreateOptions_ThrowsWhenNoVariableNamesAreSupplied()
        => Assert.Throws<ArgumentException>(() => PostgresDesignTime.CreateOptions<TestDbContext>([], TestPostgresOptions.Schema));
}
