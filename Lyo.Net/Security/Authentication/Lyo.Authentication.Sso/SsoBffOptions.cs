using System.Diagnostics;

namespace Lyo.Authentication.Sso;

/// <summary>
/// Tunables for the SSO browser flow: which return URLs may receive the handoff redirect, the fallback when none is supplied, and how long a handoff code lives. Bound from
/// <c>LyoSso</c>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SsoBffOptions
{
    /// <summary>Configuration section name (<c>LyoSso</c>).</summary>
    public const string SectionName = "LyoSso";

    /// <summary>
    /// Origin allow-list for absolute <c>returnUrl</c> values handed to <c>GET /auth/login/{provider}</c>. Each entry is a <c>scheme://host[:port]</c> string compared
    /// case-insensitively. Off-list absolute URLs are rejected at login (400). Relative paths starting with <c>/</c> are always allowed.
    /// </summary>
    public IList<string> AllowedReturnOrigins { get; set; } = [];

    /// <summary>Where to send the browser when no usable <c>returnUrl</c> is supplied. Default <c>/</c>.</summary>
    public string DefaultReturnUrl { get; set; } = "/";

    /// <summary>Lifetime of a handoff code. Default 30 seconds.</summary>
    public TimeSpan HandoffCodeTtl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How many return origins are allowed, the fallback path, and the handoff lifetime.</summary>
    public override string ToString() => $"AllowedReturnOrigins={AllowedReturnOrigins.Count} DefaultReturnUrl={DefaultReturnUrl} HandoffCodeTtl={HandoffCodeTtl}";
}