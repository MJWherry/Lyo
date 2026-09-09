namespace Lyo.Authentication.Postgres.Database;

/// <summary>Entity for the <c>[user].[claim]</c> table (admin-assigned extra JWT claims).</summary>
public sealed class UserClaimEntity
{
    /// <summary>Stable row id. PK.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning Lyo user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Claim type (JWT name).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Claim value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>When the row was first written.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
