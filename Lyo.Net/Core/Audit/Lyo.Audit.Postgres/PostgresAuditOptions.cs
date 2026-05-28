using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Audit.Postgres;

/// <summary>Configuration options for PostgreSQL audit recorder.</summary>
public sealed class PostgresAuditOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresAudit";
    public const string Schema = "audit";

    /// <summary>Per-feature tenancy policy. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Audit rows have a nullable <c>tenant_id</c> column, so all three tenancy modes are valid.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}