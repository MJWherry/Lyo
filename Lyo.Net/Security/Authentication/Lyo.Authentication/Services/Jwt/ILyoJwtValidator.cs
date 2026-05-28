using System.Security.Claims;

namespace Lyo.Authentication.Services.Jwt;

/// <summary>Validates Lyo-signed JWTs. Returns <c>null</c> on any failure — never leaks why to the caller.</summary>
public interface ILyoJwtValidator
{
    /// <summary>Validates <paramref name="jwt" /> against the configured issuer, audience, signing keys, and lifetime.</summary>
    Task<ClaimsPrincipal?> ValidateAsync(string jwt, CancellationToken ct = default);
}