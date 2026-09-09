using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.ChangeTracker.Postgres;

/// <summary>Settings for PostgreSQL change tracking.</summary>
public sealed class PostgresChangeTrackerOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresChangeTracker";
    public const string Schema = "change_tracker";

    /// <summary>Tenancy policy for this feature. Properties left unset fall back to <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Change rows keep <c>tenant_id</c> nullable, so every tenancy mode is allowed.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}