using Lyo.Postgres;

namespace Lyo.Music.Postgres;

/// <summary>Settings that control the PostgreSQL music store.</summary>
public sealed class PostgresMusicOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresMusic";

    public const string Schema = "music";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}