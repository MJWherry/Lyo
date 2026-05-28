using System.Collections.Generic;
using System.Security.Claims;

namespace Lyo.Authentication.Client;

/// <summary>
/// Forwarder kept for backward compatibility. The canonical implementation now lives in <see cref="Records.LyoJwtClaimsParser"/> so it can be shared with the WASM auth runtime.
/// </summary>
public static class LyoJwtClaimsParser
{
    /// <inheritdoc cref="Records.LyoJwtClaimsParser.Parse(string)"/>
    public static IReadOnlyList<Claim> Parse(string jwt) => Models.Records.LyoJwtClaimsParser.Parse(jwt);
}
