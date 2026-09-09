using Lyo.Postgres;

namespace Lyo.Geolocation.Postgres;

/// <summary>Settings for the PostgreSQL geolocation store.</summary>
public sealed class PostgresGeolocationOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresGeolocation";
    public const string Schema = "geolocation";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}