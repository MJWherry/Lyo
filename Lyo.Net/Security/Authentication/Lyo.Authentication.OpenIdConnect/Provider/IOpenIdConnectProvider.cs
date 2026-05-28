namespace Lyo.Authentication.OpenIdConnect.Provider;

/// <summary>
/// Describes a single external OIDC provider (Google, a specific Keycloak realm, etc.). Used by the coordinator and discovery cache. Per-provider packages (Google, Keycloak)
/// implement this interface.
/// </summary>
public interface IOpenIdConnectProvider
{
    /// <summary>Canonical name, used as the URL segment (e.g. <c>/auth/login/google</c>). Also stored on <c>[user].[linked_identity].provider</c>.</summary>
    string Name { get; }

    /// <summary>The OIDC discovery URL (e.g. <c>https://accounts.google.com/.well-known/openid-configuration</c>).</summary>
    string DiscoveryUrl { get; }

    /// <summary>The Lyo API's registered OIDC client id at this provider.</summary>
    string ClientId { get; }

    /// <summary>The Lyo API's client secret.</summary>
    string ClientSecret { get; }

    /// <summary>The redirect URI registered with the provider (must match Lyo's callback exactly).</summary>
    string RedirectUri { get; }

    /// <summary>Scopes to request from the provider (typically <c>openid email profile</c> plus provider-specific ones like <c>roles</c>).</summary>
    IReadOnlyList<string> Scopes { get; }

    /// <summary>Extra parameters appended to the authorize URL (e.g. <c>hd=mycorp.com</c> for Google Workspace).</summary>
    IReadOnlyDictionary<string, string> ExtraAuthorizeParameters { get; }

    /// <summary>Maps the validated id_token claims into a tuple consumed by the coordinator: <c>(displayName, email, emailVerified, avatarUrl, locale, providerScopes)</c>.</summary>
    OidcClaimMappingResult MapClaims(IReadOnlyDictionary<string, object?> claims);

    /// <summary>Optional pre-flight rejection (e.g. Google hosted-domain mismatch). Return <c>null</c> to accept, a string failure reason to reject.</summary>
    string? PreflightReject(IReadOnlyDictionary<string, object?> claims);
}

/// <summary>The result of mapping provider id_token claims into the Lyo user shape.</summary>
/// <param name="DisplayName">Display name from <c>name</c> (or fallback).</param>
/// <param name="Email">Email from <c>email</c> claim (may be <c>null</c> for some providers).</param>
/// <param name="EmailVerified">Provider's <c>email_verified</c> claim.</param>
/// <param name="AvatarUrl">Optional avatar from <c>picture</c> claim.</param>
/// <param name="PreferredLanguageBcp47">Optional BCP-47 language tag from <c>locale</c> claim.</param>
/// <param name="ProviderScopes">Provider-derived scopes (e.g. Keycloak realm-role -> scope mapping).</param>
public sealed record OidcClaimMappingResult(
    string DisplayName,
    string? Email,
    bool EmailVerified,
    string? AvatarUrl,
    string? PreferredLanguageBcp47,
    IReadOnlyList<string> ProviderScopes);