using System.Diagnostics;

namespace Lyo.Authentication.Saml;

/// <summary>Parsed IdP metadata: issuer, HTTP-Redirect sign-on URL, and signing certificates.</summary>
/// <param name="EntityId">IdP entityID.</param>
/// <param name="RedirectSsoUrl">SingleSignOnService location whose binding is HTTP-Redirect.</param>
/// <param name="SigningCertificates">DER bytes of signing certificates, including rollover certs.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record SamlIdpMetadata(string EntityId, string RedirectSsoUrl, IReadOnlyList<byte[]> SigningCertificates)
{
    /// <summary>Issuer, sign-on URL, and certificate count. Certificate bytes are omitted.</summary>
    public override string ToString() => $"EntityId={EntityId} Redirect={RedirectSsoUrl} Certs={SigningCertificates.Count}";
}