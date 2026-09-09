namespace Lyo.Authentication.Models.Request;

/// <summary>Create or update payload for a user claim row.</summary>
public sealed class AuthClaimReq
{
    /// <summary>Owning Lyo user. Required on create.</summary>
    public Guid UserId { get; set; }

    /// <summary>Claim type (JWT name).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Claim value.</summary>
    public string Value { get; set; } = string.Empty;
}
