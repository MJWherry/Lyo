using Microsoft.AspNetCore.Authentication;

namespace Lyo.Authentication.Client;

/// <summary>
/// Authentication scheme options for <see cref="LyoAuthCookieAuthenticationHandler" />. No knobs today — kept as a typed options class so scheme registration looks
/// ordinary.
/// </summary>
public sealed class LyoAuthCookieOptions : AuthenticationSchemeOptions { }