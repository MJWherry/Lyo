using System.Diagnostics;

namespace Lyo.Authentication.Models.Records;

/// <summary>The result of <see cref="Services.Jwt.ILyoJwtIssuer.IssueAsync" />: an access JWT, a paired refresh token, and their expirations.</summary>
/// <param name="AccessToken">The compact-serialized Lyo JWT (<c>header.payload.signature</c>).</param>
/// <param name="AccessTokenJti">The <c>jti</c> of <paramref name="AccessToken" />. Used to correlate refresh tokens to their access tokens.</param>
/// <param name="AccessTokenExpiresAt">When <paramref name="AccessToken" /> expires.</param>
/// <param name="RefreshToken">Wire-form refresh token (Format-B <c>internal</c> kind), or <c>null</c> when the issuer is configured not to mint one.</param>
/// <param name="RefreshTokenExpiresAt">When <paramref name="RefreshToken" /> expires, or <c>null</c> when no refresh token was minted.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record IssuedLyoJwt(string AccessToken, string AccessTokenJti, DateTime AccessTokenExpiresAt, string? RefreshToken, DateTime? RefreshTokenExpiresAt)
{
    public override string ToString()
        => $"IssuedLyoJwt: jti={AccessTokenJti}, accessExpires={AccessTokenExpiresAt:O}, hasRefresh={RefreshToken is not null}";
}