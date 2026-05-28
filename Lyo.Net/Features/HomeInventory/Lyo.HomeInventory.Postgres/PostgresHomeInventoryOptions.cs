using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.HomeInventory.Postgres;

/// <summary>Configuration for the home-inventory PostgreSQL schema.</summary>
public sealed class PostgresHomeInventoryOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresHomeInventory";
    public const string Schema = "home_inventory";

    /// <summary>Per-feature tenancy policy. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Every home-inventory entity has a nullable <c>tenant_id</c> column, so all three tenancy modes are valid.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    public string ConnectionString { get; set; } = string.Empty;

    public bool EnableAutoMigrations { get; set; }

    string IPostgresMigrationConfig.Schema => Schema;
}