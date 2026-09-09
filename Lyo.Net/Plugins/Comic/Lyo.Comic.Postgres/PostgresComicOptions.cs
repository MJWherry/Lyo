using Lyo.Postgres;

namespace Lyo.Comic.Postgres;

/// <summary>Settings that control the PostgreSQL comic store.</summary>
public sealed class PostgresComicOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresComic";
    public const string Schema = "comic";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}