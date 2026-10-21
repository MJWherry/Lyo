using Lyo.Postgres;

namespace Lyo.Book.Postgres;

/// <summary>Settings that control the PostgreSQL book store.</summary>
public sealed class PostgresBookOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresBook";
    public const string Schema = "book";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}