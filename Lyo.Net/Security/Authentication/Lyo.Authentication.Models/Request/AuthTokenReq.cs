namespace Lyo.Authentication.Models.Request;

/// <summary>Update payload for an opaque token. Create is not mapped; minting stays <c>POST /tokens</c>.</summary>
public sealed class AuthTokenReq
{
    /// <summary>User-facing label.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>When set, validators reject the token.</summary>
    public DateTime? RevokedTimestamp { get; set; }

    /// <summary>Audit context for the revocation.</summary>
    public string? RevokedReason { get; set; }
}
