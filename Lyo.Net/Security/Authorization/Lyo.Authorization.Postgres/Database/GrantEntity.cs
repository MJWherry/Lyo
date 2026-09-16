namespace Lyo.Authorization.Postgres.Database;

/// <summary>Row in <c>authorization.grant</c>.</summary>
public sealed class GrantEntity
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Opaque subject.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Action.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Resource type.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Null covers every id.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Null covers every tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Who saved the row.</summary>
    public string? GrantedBy { get; set; }

    /// <summary>Null does not expire.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Set when revoked or superseded.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Insert time.</summary>
    public DateTime CreatedTimestamp { get; set; }
}
