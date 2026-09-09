namespace Lyo.Authentication.Postgres.Database;

/// <summary>Entity for the <c>[user].[scope]</c> table (admin-assigned JWT authorization scopes).</summary>
public sealed class UserScopeEntity
{
    /// <summary>Stable row id. PK.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning Lyo user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Scope name (JWT <c>scope</c> token).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>When the row was first written.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
