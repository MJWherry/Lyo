using Lyo.Postgres;

namespace Lyo.Sms.Postgres;

/// <summary>Settings for PostgreSQL SMS logging.</summary>
public sealed class PostgresSmsOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresSms";
    public const string Schema = "sms";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}