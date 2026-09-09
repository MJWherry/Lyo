using Lyo.Authentication.OpenIdConnect.Provider;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
namespace Lyo.Authentication.Google;

/// <summary>Google <see cref="IOpenIdConnectProvider" /> profile. Configuration comes from <see cref="GoogleOptions" />.</summary>
public sealed class GoogleOpenIdConnectProvider : IOpenIdConnectProvider
{
    private readonly GoogleOptions _options;

    /// <summary>Builds a Google provider using <see cref="GoogleOptions" />.</summary>
    public GoogleOpenIdConnectProvider(GoogleOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.ClientId, nameof(_options.ClientId));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.ClientSecret, nameof(_options.ClientSecret));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.RedirectUri, nameof(_options.RedirectUri));
    }

    /// <inheritdoc />
    public string Name => _options.Name;

    /// <inheritdoc />
    public string DiscoveryUrl => GoogleOptions.DiscoveryUrl;

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
    public OidcClaimMappingResult MapClaims(IReadOnlyDictionary<string, object?> claims) => GoogleClaimMapper.Map(claims);

    /// <inheritdoc />
    public string? PreflightReject(IReadOnlyDictionary<string, object?> claims)
    {
        if (_options.HostedDomain.IsNullOrWhitespace())
            return null;

        if (!claims.TryGetValue("hd", out var raw) || raw is null)
            return "HostedDomainMismatch";

        var actual = raw.ToString();
        if (!string.Equals(actual, _options.HostedDomain, StringComparison.OrdinalIgnoreCase))
            return "HostedDomainMismatch";

        return null;
    }
}