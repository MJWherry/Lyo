using Lyo.Exceptions;

namespace Lyo.Authentication.Keycloak;

/// <summary>Settings for a Keycloak realm used as an OpenID Connect provider.</summary>
public sealed class KeycloakOptions
{
    /// <summary>Section name used when nothing else is specified.</summary>
    public const string SectionName = "KeycloakAuth";

    /// <summary>Keycloak origin with no trailing slash and no <c>/realms</c> suffix. Example: <c>https://sso.lyolabs.io</c>.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Realm id. Joined with <see cref="BaseUrl" /> to form the discovery URL.</summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>OAuth client id configured on the Keycloak client.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth client secret for this Keycloak client.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Redirect URI registered on this client.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Requested scopes. Falls back to <c>openid email profile roles</c>.</summary>
    public IList<string> Scopes { get; set; } = ["openid", "email", "profile", "roles"];

    /// <summary>
    /// Translates Keycloak realm-role names into Lyo scopes. A role may expand to several scopes; roles missing from this map are ignored so a new Keycloak role
    /// cannot silently grant Lyo permissions.
    /// </summary>
    public IDictionary<string, string[]> RolesToScopes { get; set; } = new Dictionary<string, string[]>(StringComparer.Ordinal);

    /// <summary>Optional replacement for the registered provider name. Falls back to <c>keycloak:&lt;realm&gt;</c>.</summary>
    public string? Name { get; set; }

    /// <summary>Throws when BaseUrl, Realm, ClientId, ClientSecret, or RedirectUri is missing.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(BaseUrl);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Realm);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ClientId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ClientSecret);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(RedirectUri);
    }
}