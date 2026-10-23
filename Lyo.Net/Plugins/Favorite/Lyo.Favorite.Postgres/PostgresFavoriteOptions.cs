using System.Diagnostics;
using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Favorite.Postgres;

/// <summary>Settings that control PostgreSQL favorite store.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PostgresFavoriteOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresFavorite";
    public const string Schema = "favorite";

    /// <summary>Tenancy rules for this feature. Properties left unset come from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Favorite rows require a non-null <c>tenant_id</c>; <see cref="TenancyMode.SystemOnly" /> is rejected when the store is constructed.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;

    /// <summary>Schema, auto-migrate flag, and tenancy. The connection string is omitted.</summary>
    public override string ToString() => $"{base.ToString()} Tenancy={Tenancy}";
}