using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.HomeInventory.Postgres;

/// <summary>Settings for the home-inventory PostgreSQL schema.</summary>
public sealed class PostgresHomeInventoryOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresHomeInventory";
    public const string Schema = "home_inventory";

    /// <summary>Tenancy rules for this feature. Properties left unset come from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Home-inventory entities expose a nullable <c>tenant_id</c>, so any of the three tenancy modes is allowed.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}