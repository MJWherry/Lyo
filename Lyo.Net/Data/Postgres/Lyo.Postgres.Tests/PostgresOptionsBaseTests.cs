namespace Lyo.Postgres.Tests;

public class PostgresOptionsBaseTests
{
    [Fact]
    public void Schema_SurfacesTheDerivedSchemaName()
    {
        var options = new TestPostgresOptions();
        Assert.Equal(TestPostgresOptions.Schema, ((IPostgresMigrationConfig)options).Schema);
    }

    [Fact]
    public void EnableAutoMigrations_DefaultsToFalse() => Assert.False(new TestPostgresOptions().EnableAutoMigrations);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ThrowsWhenConnectionStringIsMissing(string connectionString)
        => Assert.Throws<ArgumentException>(() => new TestPostgresOptions { ConnectionString = connectionString }.Validate());

    [Fact]
    public void Validate_AcceptsAConnectionString() => new TestPostgresOptions { ConnectionString = "Host=localhost;Database=lyo" }.Validate();

    [Fact]
    public void Validate_OverrideStillEnforcesTheBaseConnectionStringCheck()
        => Assert.Throws<ArgumentException>(() => new TestPostgresOptionsWithExtraValidation().Validate());

    [Fact]
    public void Validate_OverrideEnforcesItsOwnChecks()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new TestPostgresOptionsWithExtraValidation { ConnectionString = "Host=localhost", CommandTimeoutSeconds = 0 }.Validate());
}
