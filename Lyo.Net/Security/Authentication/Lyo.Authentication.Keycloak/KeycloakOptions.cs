using System.Collections.Generic;

namespace Lyo.Authentication.Keycloak;

/// <summary>Configuration for a Keycloak realm acting as an OIDC provider.</summary>
public sealed class KeycloakOptions
{
    /// <summary>Default configuration section.</summary>
    public const string SectionName = "KeycloakAuth";

    /// <summary>Keycloak server base URL (no trailing slash, no <c>/realms</c> suffix). Example: <c>https://sso.lyolabs.io</c>.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Realm name. Combined with <see cref="BaseUrl"/> to derive the discovery URL.</summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>OAuth client id (set on the Keycloak client).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>The redirect URI registered for this client.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Scopes to request. Defaults to <c>openid email profile roles</c>.</summary>
    public IList<string> Scopes { get; set; } = ["openid", "email", "profile", "roles"];

    /// <summary>
    /// Maps Keycloak realm-role names to Lyo scope names. Each realm role can map to one or more scopes; a role that is not present in this dictionary is silently dropped so adding a
    /// Keycloak role cannot accidentally grant Lyo permissions.
    /// </summary>
    public IDictionary<string, string[]> RolesToScopes { get; set; } = new Dictionary<string, string[]>(System.StringComparer.Ordinal);

    /// <summary>Optional override of the registered provider name. Defaults to <c>keycloak:&lt;realm&gt;</c>.</summary>
    public string? Name { get; set; }
}
