using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Audit.Postgres;

/// <summary>Settings for the PostgreSQL audit recorder.</summary>
public sealed class PostgresAuditOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresAudit";
    public const string Schema = "audit";

    /// <summary>Tenancy policy for this feature. Properties left unset fall back to <see cref="EntityRefOptions" />.</summary>
    /// <remarks>The audit tables keep <c>tenant_id</c> nullable, so every tenancy mode is allowed.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}