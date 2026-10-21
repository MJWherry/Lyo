using Lyo.Postgres;

namespace Lyo.Podcast.Postgres;

/// <summary>Settings that control the PostgreSQL podcast store.</summary>
public sealed class PostgresPodcastOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresPodcast";

    public const string Schema = "podcast";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}