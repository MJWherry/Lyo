using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Billing.Postgres;

/// <summary>Settings for the Billing PostgreSQL schema.</summary>
public sealed class PostgresBillingOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresBilling";

    /// <summary>PostgreSQL schema.</summary>
    public const string Schema = "billing";

    /// <summary>Tenancy rules for this feature. Properties left unset come from <see cref="EntityRefOptions" />.</summary>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}