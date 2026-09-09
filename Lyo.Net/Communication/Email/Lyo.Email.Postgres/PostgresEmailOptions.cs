using Lyo.Postgres;

namespace Lyo.Email.Postgres;

/// <summary>Settings for PostgreSQL email logging. This package never sends or fetches mail.</summary>
public sealed class PostgresEmailOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name (<c>PostgresEmail</c>).</summary>
    public const string SectionName = "PostgresEmail";

    /// <summary>PostgreSQL schema for email log tables (<c>email</c>).</summary>
    public const string Schema = "email";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}