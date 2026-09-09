namespace Lyo.Authentication.AspNetCore.Defaults;

/// <summary>ASP.NET authentication scheme names registered by <c>AddLyoApiTokenAuthentication</c>.</summary>
public static class LyoAuthenticationSchemes
{
    /// <summary>Opaque-token scheme: validates Format-B Lyo tokens (<c>lyo_...</c>).</summary>
    public const string OpaqueToken = "LyoApiToken";

    /// <summary>Lyo-JWT scheme: validates Ed25519-signed JWTs minted by the Lyo API.</summary>
    public const string LyoJwt = "LyoJwt";

    /// <summary>Dispatcher policy scheme: sniffs the credential prefix and forwards to <see cref="OpaqueToken" /> or <see cref="LyoJwt" />.</summary>
    public const string Bearer = "LyoBearer";
}