using Lyo.Exceptions;

namespace Lyo.Authentication.Google;

/// <summary>Settings that drive the Google OpenID Connect provider profile.</summary>
public sealed class GoogleOptions
{
    /// <summary>Section name used when nothing else is specified.</summary>
    public const string SectionName = "GoogleAuth";

    /// <summary>Provider id written into URLs and audit rows (<c>google</c>).</summary>
    public const string DefaultName = "google";

    /// <summary>Google OpenID Connect discovery endpoint. Leave it alone unless you have a rare override.</summary>
    public const string DiscoveryUrl = "https://accounts.google.com/.well-known/openid-configuration";

    /// <summary>OAuth client id issued in the Google Cloud console.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth client secret for this Google app.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Redirect URI registered on the client; the value must match character-for-character.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Optional Workspace hosted domain (<c>hd</c>). When present, login fails unless the id_token's domain matches.</summary>
    public string? HostedDomain { get; set; }

    /// <summary>Requested scopes. Falls back to <c>openid email profile</c>.</summary>
    public IList<string> Scopes { get; set; } = ["openid", "email", "profile"];

    /// <summary>Optional replacement for the registered <see cref="DefaultName">name</see>. Reserve this for cases such as multi-tenant Google setups.</summary>
    public string Name { get; set; } = DefaultName;

    /// <summary>Throws when ClientId, ClientSecret, or RedirectUri is missing.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ClientId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ClientSecret);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(RedirectUri);
    }
}