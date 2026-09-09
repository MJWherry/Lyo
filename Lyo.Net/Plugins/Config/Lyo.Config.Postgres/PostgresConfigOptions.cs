using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Config.Postgres;

/// <summary>Settings that control PostgreSQL config storage.</summary>
public sealed class PostgresConfigOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresConfig";
    public const string Schema = "config";

    /// <summary>Tenancy rules for binding/revision rows. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Binding and revision rows allow a null <c>tenant_id</c>, so any tenancy mode works. Definitions stay deployment-global and ignore tenancy.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}