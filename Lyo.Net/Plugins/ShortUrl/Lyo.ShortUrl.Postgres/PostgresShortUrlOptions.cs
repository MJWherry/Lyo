using Lyo.Postgres;

namespace Lyo.ShortUrl.Postgres;

/// <summary>Settings that control PostgreSQL URL shortener service.</summary>
public sealed class PostgresShortUrlOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresShortUrl";
    public const string Schema = "url";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}