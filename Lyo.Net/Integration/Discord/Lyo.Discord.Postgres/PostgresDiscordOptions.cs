using Lyo.Postgres;

namespace Lyo.Discord.Postgres;

/// <summary>Settings that control the PostgreSQL Discord database.</summary>
public sealed class PostgresDiscordOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresDiscord";
    public const string Schema = "discord";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}