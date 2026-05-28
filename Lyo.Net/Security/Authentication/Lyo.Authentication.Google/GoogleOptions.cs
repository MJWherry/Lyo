namespace Lyo.Authentication.Google;

/// <summary>Configuration for the Google OIDC provider profile.</summary>
public sealed class GoogleOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "GoogleAuth";

    /// <summary>Default provider name used in URLs and audit records (<c>google</c>).</summary>
    public const string DefaultName = "google";

    /// <summary>Google's OIDC discovery URL. Fixed and not normally overridden.</summary>
    public const string DiscoveryUrl = "https://accounts.google.com/.well-known/openid-configuration";

    /// <summary>OAuth client id (from the Google Cloud console).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>The redirect URI registered for this client (must match exactly).</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Optional Google Workspace hosted domain (<c>hd</c> claim). When set, login is rejected unless the user's id_token reports a matching domain.</summary>
    public string? HostedDomain { get; set; }

    /// <summary>Scopes to request. Defaults to <c>openid email profile</c>.</summary>
    public IList<string> Scopes { get; set; } = ["openid", "email", "profile"];

    /// <summary>Optional override of the registered provider <see cref="DefaultName">name</see>. Use sparingly (e.g. for multi-tenant Google deployments).</summary>
    public string Name { get; set; } = DefaultName;
}