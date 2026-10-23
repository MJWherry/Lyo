using System.Diagnostics;
using Lyo.Authentication.Saml;
using Lyo.Exceptions;

namespace Lyo.Authentication.Okta;

/// <summary>
/// Okta SAML app settings. <see cref="AppId" /> is the public <c>exk…</c> key from "View IdP metadata", not the <c>0oa…</c> application id. The IdP Identifier / Audience and
/// the Single sign-on URL must match <see cref="EntityId" /> and <see cref="AcsUrl" /> exactly. Leave request signing and assertion encryption off.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class OktaOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "OktaAuth";

    /// <summary>Provider name registered on the SSO host.</summary>
    public const string DefaultName = "okta";

    /// <summary>Okta host (<c>{org}.okta.com</c>, <c>{org}.oktapreview.com</c>, or a custom domain) with no scheme and no path.</summary>
    public string? Domain { get; set; }

    /// <summary>Public metadata key (<c>exk…</c>).</summary>
    public string? AppId { get; set; }

    /// <summary>Service-provider entity id. Must match the Okta Audience.</summary>
    public string? EntityId { get; set; }

    /// <summary>Assertion consumer URL. Must match the Okta Single sign-on URL.</summary>
    public string? AcsUrl { get; set; }

    /// <summary>Attribute that carries the email. Default <c>email</c>. Only this attribute counts as verified.</summary>
    public string EmailAttributeName { get; set; } = "email";

    /// <summary>Attribute that carries the Okta user id (<c>00u…</c>) when it is not the NameID. <c>user.id</c> is the usual name.</summary>
    public string? SubjectAttributeName { get; set; }

    /// <summary>IdP metadata URL built from <see cref="Domain" /> and <see cref="AppId" />.</summary>
    public string MetadataUrl => $"https://{Domain}/app/{AppId}/sso/saml/metadata";

    /// <summary>Rejects a missing domain, a domain that contains a scheme or path, a missing or non-<c>exk</c> app id, and an ACS URL that is not allowed.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Domain);
        if (Domain.Contains("://", StringComparison.Ordinal) || Domain.Contains('/'))
            throw new ArgumentException("Okta domain must be a host with no scheme and no path.", nameof(Domain));

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(AppId);
        if (!AppId.StartsWith("exk", StringComparison.Ordinal))
            throw new ArgumentException("Okta app id must be the public exk metadata key, not the 0oa application id.", nameof(AppId));

        SamlServiceProviderRules.Validate(EntityId, AcsUrl);
    }

    /// <summary>Okta host, app id, and assertion consumer URL.</summary>
    public override string ToString() => $"Domain={Domain} AppId={AppId} EntityId={EntityId} AcsUrl={AcsUrl}";
}