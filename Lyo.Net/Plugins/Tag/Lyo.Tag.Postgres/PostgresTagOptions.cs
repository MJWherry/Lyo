using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Tag.Postgres;

/// <summary>Settings that control PostgreSQL tag store.</summary>
public sealed class PostgresTagOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresTag";
    public const string Schema = "tag";

    /// <summary>Tenancy rules for this feature. Properties left unset come from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Tag rows require a non-null <c>tenant_id</c>; <see cref="TenancyMode.SystemOnly" /> is rejected when the store is constructed.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}