using Lyo.Exceptions;

namespace Lyo.Authentication.Saml;

/// <summary>Checks shared service-provider settings: entity id and assertion consumer URL.</summary>
public static class SamlServiceProviderRules
{
    /// <summary>
    /// Rejects a missing entity id, a missing ACS URL, a non-absolute ACS URL, and an <c>http</c> ACS URL whose host is not localhost.
    /// </summary>
    public static void Validate(string? entityId, string? acsUrl)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(acsUrl);
        if (!Uri.TryCreate(acsUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("The SAML ACS URL must be an absolute HTTP or HTTPS URL.", nameof(acsUrl));

        if (uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
            throw new ArgumentException("An http SAML ACS URL is only allowed for localhost.", nameof(acsUrl));
    }
}
