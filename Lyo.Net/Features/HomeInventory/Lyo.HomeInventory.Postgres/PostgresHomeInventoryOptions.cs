using Lyo.Postgres;

namespace Lyo.HomeInventory.Postgres;

/// <summary>Configuration for the home-inventory PostgreSQL schema.</summary>
public sealed class PostgresHomeInventoryOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresHomeInventory";
    public const string Schema = "home_inventory";

    public string ConnectionString { get; set; } = string.Empty;

    public bool EnableAutoMigrations { get; set; }

    string IPostgresMigrationConfig.Schema => Schema;
}