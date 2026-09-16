using Lyo.Postgres;

namespace Lyo.Story.Postgres;

/// <summary>Settings that control the PostgreSQL story store.</summary>
public sealed class PostgresStoryOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresStory";
    public const string Schema = "story";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}
