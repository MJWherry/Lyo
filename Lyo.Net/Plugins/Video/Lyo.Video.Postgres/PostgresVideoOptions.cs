using Lyo.Postgres;

namespace Lyo.Video.Postgres;

/// <summary>Settings that control the PostgreSQL video store.</summary>
public sealed class PostgresVideoOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresVideo";

    public const string Schema = "video";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}