using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Comment.Postgres;

/// <summary>Settings that control PostgreSQL comment store.</summary>
public sealed class PostgresCommentOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresComment";
    public const string Schema = "comment";

    /// <summary>Tenancy rules for this feature. Properties left unset come from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Comment rows require a non-null <c>tenant_id</c>; <see cref="TenancyMode.SystemOnly" /> is rejected when the store is constructed.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}