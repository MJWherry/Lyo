using System.Security.Claims;

namespace Lyo.Authentication.Client;

/// <summary>
/// Forwarder kept for backward compatibility. The canonical implementation now lives in <see cref="Records.LyoJwtClaimsParser" /> so the WASM auth
/// runtime can share it.
/// </summary>
public static class LyoJwtClaimsParser
{
    /// <inheritdoc cref="Records.LyoJwtClaimsParser.Parse(string)" />
    public static IReadOnlyList<Claim> Parse(string jwt) => Models.Records.LyoJwtClaimsParser.Parse(jwt);
}