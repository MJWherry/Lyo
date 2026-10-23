using Lyo.Postgres;

namespace Lyo.Authorization.Postgres;

/// <summary>Connection settings for the authorization schema.</summary>
public sealed class PostgresAuthorizationOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresAuthorization";

    /// <summary>Schema that owns <c>grant</c>.</summary>
    public const string Schema = "authorization";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}