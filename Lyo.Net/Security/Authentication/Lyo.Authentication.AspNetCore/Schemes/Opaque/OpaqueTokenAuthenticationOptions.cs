using Lyo.Common.Core.Net;
using Microsoft.AspNetCore.Authentication;

namespace Lyo.Authentication.AspNetCore.Schemes.Opaque;

/// <summary>Options for the opaque-token authentication handler.</summary>
public sealed class OpaqueTokenAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Header that carries the bearer credential. Default = <c>Authorization</c>.</summary>
    public string HeaderName { get; set; } = LyoHttpHeaders.Authorization;

    /// <summary>Expected scheme inside <see cref="HeaderName" />. Default = <c>Bearer</c>.</summary>
    public string Scheme { get; set; } = "Bearer";

    /// <summary>Alternate header that may carry the credential with no scheme prefix. Default = <c>X-Api-Key</c>. Set <c>null</c> to disable.</summary>
    public string? AlsoAccept { get; set; } = LyoHttpHeaders.ApiKey;
}