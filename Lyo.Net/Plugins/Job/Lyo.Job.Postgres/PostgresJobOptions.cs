using Lyo.Postgres;

namespace Lyo.Job.Postgres;

/// <summary>Settings for the PostgreSQL job management database.</summary>
public sealed class PostgresJobOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresJob";
    public const string Schema = "job";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}