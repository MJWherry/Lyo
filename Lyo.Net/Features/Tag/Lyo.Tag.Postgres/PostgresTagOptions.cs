using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Tag.Postgres;

/// <summary>Configuration options for PostgreSQL tag store.</summary>
public sealed class PostgresTagOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresTag";
    public const string Schema = "tag";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <summary>Per-feature tenancy policy. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>The tag store is backed by a non-null <c>tenant_id</c> column; <see cref="TenancyMode.SystemOnly" /> is rejected at store-construction time.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}