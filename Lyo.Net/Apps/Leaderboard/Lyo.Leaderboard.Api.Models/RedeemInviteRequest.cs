namespace Lyo.Leaderboard.Api.Models;

/// <summary>Redeem a plaintext invite token.</summary>
public sealed class RedeemInviteRequest
{
    /// <summary>Plaintext token from the join link.</summary>
    public string Token { get; set; } = string.Empty;
}
