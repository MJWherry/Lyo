using System.Diagnostics;
using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Rating.Postgres;

/// <summary>Settings that control PostgreSQL rating store.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PostgresRatingOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresRating";
    public const string Schema = "rating";

    /// <summary>Tenancy rules for this feature. Properties left unset come from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Rating rows require a non-null <c>tenant_id</c>; <see cref="TenancyMode.SystemOnly" /> is rejected when the store is constructed.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;

    /// <summary>Schema, auto-migrate flag, and tenancy. The connection string is omitted.</summary>
    public override string ToString() => $"{base.ToString()} Tenancy={Tenancy}";
}