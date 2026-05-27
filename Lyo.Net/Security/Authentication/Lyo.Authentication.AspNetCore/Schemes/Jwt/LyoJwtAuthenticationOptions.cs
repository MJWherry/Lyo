using Microsoft.AspNetCore.Authentication;

namespace Lyo.Authentication.AspNetCore.Schemes.Jwt;

/// <summary>Options for the Lyo-JWT authentication handler.</summary>
public sealed class LyoJwtAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>The header to read the bearer credential from. Default = <c>Authorization</c>.</summary>
    public string HeaderName { get; set; } = "Authorization";

    /// <summary>The expected scheme inside <see cref="HeaderName"/>. Default = <c>Bearer</c>.</summary>
    public string Scheme { get; set; } = "Bearer";
}
