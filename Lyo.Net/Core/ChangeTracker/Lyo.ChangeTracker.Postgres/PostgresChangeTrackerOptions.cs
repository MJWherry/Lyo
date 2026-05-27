using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.ChangeTracker.Postgres;

/// <summary>Configuration options for PostgreSQL change tracking.</summary>
public sealed class PostgresChangeTrackerOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresChangeTracker";
    public const string Schema = "change_tracker";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <summary>Per-feature tenancy policy. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Change rows have a nullable <c>tenant_id</c> column, so all three tenancy modes are valid.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    string IPostgresMigrationConfig.Schema => Schema;
}