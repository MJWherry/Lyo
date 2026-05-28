using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Exceptions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Keycloak;

/// <summary>The Keycloak realm profile for <see cref="IOpenIdConnectProvider" />.</summary>
public sealed class KeycloakOpenIdConnectProvider : IOpenIdConnectProvider
{
    private readonly KeycloakOptions _options;
    private readonly Dictionary<string, string[]> _rolesToScopes;

    /// <summary>Creates a provider from <see cref="KeycloakOptions" />.</summary>
    public KeycloakOpenIdConnectProvider(IOptions<KeycloakOptions> options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options.Value;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.BaseUrl, nameof(_options.BaseUrl));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.Realm, nameof(_options.Realm));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.ClientId, nameof(_options.ClientId));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.ClientSecret, nameof(_options.ClientSecret));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.RedirectUri, nameof(_options.RedirectUri));
        Name = string.IsNullOrWhiteSpace(_options.Name) ? $"keycloak:{_options.Realm}" : _options.Name!;
        DiscoveryUrl = $"{_options.BaseUrl.TrimEnd('/')}/realms/{_options.Realm}/.well-known/openid-configuration";
        _rolesToScopes = new(_options.RolesToScopes, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string DiscoveryUrl { get; }

    /// <inheritdoc />
    public string ClientId => _options.ClientId;

    /// <inheritdoc />
    public string ClientSecret => _options.ClientSecret;

    /// <inheritdoc />
    public string RedirectUri => _options.RedirectUri;

    /// <inheritdoc />
    public IReadOnlyList<string> Scopes => [.. _options.Scopes];

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ExtraAuthorizeParameters { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <inheritdoc />
    public OidcClaimMappingResult MapClaims(IReadOnlyDictionary<string, object?> claims) => KeycloakClaimMapper.Map(claims, _rolesToScopes);

    /// <inheritdoc />
    public string? PreflightReject(IReadOnlyDictionary<string, object?> claims) => null;
}