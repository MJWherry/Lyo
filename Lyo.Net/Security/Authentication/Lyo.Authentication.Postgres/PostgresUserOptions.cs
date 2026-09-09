using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Authentication.Postgres;

/// <summary>Settings for <c>[user]</c> schema persistence (tokens, users, linked identities).</summary>
public sealed class PostgresUserOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresUser";

    /// <summary>Schema owned by this library. All three tables live here, as does the <c>__EFMigrationsHistory</c> table.</summary>
    public const string Schema = "user";

    /// <summary>Per-feature tenancy policy. Unset properties inherit from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Authentication tables (user/token/linked-identity/user-event) all have nullable <c>tenant_id</c> columns, so all three tenancy modes are valid.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}