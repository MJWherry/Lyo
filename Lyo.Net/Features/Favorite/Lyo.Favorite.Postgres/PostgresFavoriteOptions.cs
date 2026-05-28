using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Favorite.Postgres;

/// <summary>Configuration options for PostgreSQL favorite store.</summary>
public sealed class PostgresFavoriteOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresFavorite";
    public const string Schema = "favorite";

    /// <summary>Per-feature tenancy policy. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>The favorite store is backed by a non-null <c>tenant_id</c> column; <see cref="TenancyMode.SystemOnly" /> is rejected at store-construction time.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}