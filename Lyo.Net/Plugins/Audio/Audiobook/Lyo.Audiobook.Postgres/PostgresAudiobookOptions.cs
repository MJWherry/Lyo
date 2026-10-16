using Lyo.Postgres;

namespace Lyo.Audiobook.Postgres;

/// <summary>Settings that control the PostgreSQL audiobook store.</summary>
public sealed class PostgresAudiobookOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresAudiobook";

    public const string Schema = "audiobook";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}
