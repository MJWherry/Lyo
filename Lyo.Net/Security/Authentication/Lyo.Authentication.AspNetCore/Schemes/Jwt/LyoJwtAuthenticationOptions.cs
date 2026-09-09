using Lyo.Common.Core.Net;
using Microsoft.AspNetCore.Authentication;

namespace Lyo.Authentication.AspNetCore.Schemes.Jwt;

/// <summary>Options for the Lyo-JWT authentication handler.</summary>
public sealed class LyoJwtAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Header that carries the bearer credential. Default = <c>Authorization</c>.</summary>
    public string HeaderName { get; set; } = LyoHttpHeaders.Authorization;

    /// <summary>Expected scheme inside <see cref="HeaderName" />. Default = <c>Bearer</c>.</summary>
    public string Scheme { get; set; } = "Bearer";
}