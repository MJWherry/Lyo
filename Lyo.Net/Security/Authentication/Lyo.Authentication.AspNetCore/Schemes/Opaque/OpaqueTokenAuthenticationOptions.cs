using Microsoft.AspNetCore.Authentication;

namespace Lyo.Authentication.AspNetCore.Schemes.Opaque;

/// <summary>Options for the opaque-token authentication handler.</summary>
public sealed class OpaqueTokenAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>The header to read the bearer credential from. Default = <c>Authorization</c>.</summary>
    public string HeaderName { get; set; } = "Authorization";

    /// <summary>The expected scheme inside <see cref="HeaderName" />. Default = <c>Bearer</c>.</summary>
    public string Scheme { get; set; } = "Bearer";

    /// <summary>An alternate header that may carry the credential directly (no scheme prefix). Default = <c>X-Api-Key</c>. Set to <c>null</c> to disable.</summary>
    public string? AlsoAccept { get; set; } = "X-Api-Key";
}