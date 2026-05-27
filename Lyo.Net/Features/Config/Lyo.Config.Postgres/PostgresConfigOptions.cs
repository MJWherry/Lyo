using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Config.Postgres;

/// <summary>Configuration options for PostgreSQL config storage.</summary>
public sealed class PostgresConfigOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresConfig";
    public const string Schema = "config";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <summary>Per-feature tenancy policy for binding/revision rows. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Config bindings and revisions have nullable <c>tenant_id</c> columns, so all three tenancy modes are valid. Definitions are deployment-global and ignore tenancy.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}