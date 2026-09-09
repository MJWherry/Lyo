using Lyo.Postgres;

namespace Lyo.People.Postgres;

/// <summary>Options for the PostgreSQL People store.</summary>
public sealed class PostgresPeopleOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresPeople";
    public const string Schema = "people";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}