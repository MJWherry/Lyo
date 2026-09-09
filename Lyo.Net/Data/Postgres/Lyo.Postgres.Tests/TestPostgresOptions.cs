using Microsoft.EntityFrameworkCore;

namespace Lyo.Postgres.Tests;

/// <summary>Stand-in for a package's <c>Postgres{Feature}Options</c>, using the same const-plus-override shape.</summary>
public sealed class TestPostgresOptions : PostgresOptionsBase
{
    public const string SectionName = "Lyo:TestPostgres";
    public const string Schema = "test_schema";

    protected override string SchemaName => Schema;
}

/// <summary>Options whose <see cref="Validate" /> adds a package-specific check above the base connection-string check.</summary>
public sealed class TestPostgresOptionsWithExtraValidation : PostgresOptionsBase
{
    public const string Schema = "test_schema_extra";

    public int CommandTimeoutSeconds { get; set; } = 30;

    protected override string SchemaName => Schema;

    public override void Validate()
    {
        base.Validate();
        if (CommandTimeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(CommandTimeoutSeconds), CommandTimeoutSeconds, "Command timeout must be positive.");
    }
}

/// <summary>Minimal context used to drive the shared option-building and registration helpers.</summary>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestRow> Rows => Set<TestRow>();
}

public sealed class TestRow
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
