namespace Lyo.Authentication.Postgres.Database;

/// <summary>Entity for the <c>[user].[token]</c> table (Format-B opaque tokens).</summary>
public sealed class TokenEntity
{
    /// <summary>11-char lowercase Crockford base32 id. PK.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>SHA-256 of the secret segment (32 bytes).</summary>
    public byte[] SecretHash { get; set; } = Array.Empty<byte>();

    /// <summary>One of <see cref="Format.ApiTokenKind" />.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>One of <see cref="Format.ApiTokenRing" />.</summary>
    public string Ring { get; set; } = string.Empty;

    /// <summary>Owning user (null for unowned <c>svc</c>/<c>internal</c> tokens).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Optional tenant scope. <see langword="null" /> denotes a system/service token; non-null binds the token to a specific tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>User-facing label.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Snapshot of scopes at issuance, serialized as JSON string array (Option C).</summary>
    public string ScopesJson { get; set; } = "[]";

    /// <summary>Caller-supplied metadata bag serialized as JSON object (null when none).</summary>
    public string? MetadataJson { get; set; }

    /// <summary>When the token was issued.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the row was last touched.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>When the token expires (null = no expiry).</summary>
    public DateTime? ExpiresTimestamp { get; set; }

    /// <summary>Best-effort "last validated successfully" timestamp.</summary>
    public DateTime? LastUsedTimestamp { get; set; }

    /// <summary>Hard stop. When non-null and in the past, validators reject the token.</summary>
    public DateTime? RevokedTimestamp { get; set; }

    /// <summary>Audit context for the revocation.</summary>
    public string? RevokedReason { get; set; }

    /// <summary>Previous token id when this token replaced another via rotate.</summary>
    public string? RotatedFromId { get; set; }
}