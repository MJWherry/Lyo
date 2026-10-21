namespace Lyo.Authorization;

/// <summary>A stored grant. Change it by revoking and saving again. There is no update.</summary>
public sealed record Grant
{
    /// <summary>Row id assigned by the store.</summary>
    public Guid Id { get; init; }

    /// <summary>Opaque subject. Trimmed, never lowercased.</summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>Action, compared ordinal after trim.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Resource type, compared ordinal after trim.</summary>
    public string ResourceType { get; init; } = string.Empty;

    /// <summary>Null covers every id of this type. A guid is lowercase <c>D</c>.</summary>
    public string? ResourceId { get; init; }

    /// <summary>Null covers every tenant. A set value covers that tenant only.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Who saved the grant, when known.</summary>
    public string? GrantedBy { get; init; }

    /// <summary>Null means the grant does not expire. Equal to utc now is expired.</summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>Set when the grant was revoked or superseded.</summary>
    public DateTime? RevokedAt { get; init; }

    /// <summary>When the row was inserted.</summary>
    public DateTime CreatedTimestamp { get; init; }
}