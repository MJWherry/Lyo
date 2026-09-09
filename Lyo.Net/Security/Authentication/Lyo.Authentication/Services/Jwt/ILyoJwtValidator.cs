using System.Security.Claims;

namespace Lyo.Authentication.Services.Jwt;

/// <summary>Checks Lyo-issued JWTs. Any failure returns <c>null</c>; the reason is never exposed to the caller.</summary>
public interface ILyoJwtValidator
{
    /// <summary>Validates <paramref name="jwt" /> for issuer, audience, signing keys, and lifetime.</summary>
    Task<ClaimsPrincipal?> ValidateAsync(string jwt, CancellationToken ct = default);
}