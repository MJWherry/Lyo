using Lyo.Postgres;

namespace Lyo.Endato.Postgres;

/// <summary>Settings that control PostgreSQL Endato persistence.</summary>
public sealed class PostgresEndatoOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresEndato";
    public const string Schema = "endato";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}