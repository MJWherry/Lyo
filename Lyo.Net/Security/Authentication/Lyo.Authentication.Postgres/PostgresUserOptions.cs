using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Authentication.Postgres;

/// <summary>Configuration for the <c>[user]</c> schema persistence (tokens, users, linked identities).</summary>
public sealed class PostgresUserOptions : IPostgresMigrationConfig
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresUser";

    /// <summary>Schema owned by this library. All three tables live here, as does the <c>__EFMigrationsHistory</c> table.</summary>
    public const string Schema = "user";

    /// <summary>The PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Whether to run pending migrations on host startup. Set <c>false</c> in environments where DDL is managed out of band.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <summary>Per-feature tenancy policy. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Authentication tables (user/token/linked-identity/user-event) all have nullable <c>tenant_id</c> columns, so all three tenancy modes are valid.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}
