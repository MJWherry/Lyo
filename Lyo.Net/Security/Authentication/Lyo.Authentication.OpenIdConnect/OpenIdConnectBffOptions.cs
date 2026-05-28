namespace Lyo.Authentication.OpenIdConnect;

/// <summary>
/// Tunables for the OIDC BFF surface: which external return URLs we'll redirect to after a successful login, the default fallback, and how long single-use handoff codes
/// survive. Bound from configuration via
/// <see cref="Extensions.AddLyoOpenIdConnect(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration, string)" />.
/// </summary>
public sealed class OpenIdConnectBffOptions
{
    /// <summary>Configuration section name (<c>LyoOidcBff</c>).</summary>
    public const string SectionName = "LyoOidcBff";

    /// <summary>
    /// Origin allow-list for absolute <c>returnUrl</c> values handed to <c>GET /auth/login/{provider}</c>. Each entry is a <c>scheme://host[:port]</c> string compared
    /// case-insensitively via <see cref="Uri.GetLeftPart" />(<see cref="UriPartial.Authority" />). Anything not on the list is downgraded to <see cref="DefaultReturnUrl" />. Relative
    /// paths starting with <c>/</c> are always allowed (they redirect to the API's own origin, which is only useful when API and frontend are co-located).
    /// </summary>
    public IList<string> AllowedReturnOrigins { get; set; } = [];

    /// <summary>Where to send the browser when no usable <c>returnUrl</c> is supplied or the supplied value fails the allow-list check. Default <c>/</c>.</summary>
    public string DefaultReturnUrl { get; set; } = "/";

    /// <summary>
    /// Lifetime of a <see cref="Handoff.LyoHandoffCode" />. Must be long enough for the browser redirect + Gateway server-to-server exchange to complete, short enough to limit
    /// replay windows. Default <c>30s</c>.
    /// </summary>
    public TimeSpan HandoffCodeTtl { get; set; } = TimeSpan.FromSeconds(30);
}