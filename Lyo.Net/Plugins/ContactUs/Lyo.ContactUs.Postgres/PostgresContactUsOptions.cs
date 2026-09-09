using Lyo.Postgres;

namespace Lyo.ContactUs.Postgres;

/// <summary>Settings that control PostgreSQL contact form service.</summary>
public sealed class PostgresContactUsOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresContactUs";
    public const string Schema = "contact";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}