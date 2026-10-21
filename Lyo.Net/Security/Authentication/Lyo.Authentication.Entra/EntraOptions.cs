using Lyo.Authentication.Saml;
using Lyo.Exceptions;

namespace Lyo.Authentication.Entra;

/// <summary>
/// Microsoft Entra SAML app settings. The Identifier (Entity ID) and Reply URL must match <see cref="EntityId" /> and <see cref="AcsUrl" /> exactly. Leave request
/// verification and assertion encryption off.
/// </summary>
public sealed class EntraOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "EntraAuth";

    /// <summary>Provider name registered on the SSO host.</summary>
    public const string DefaultName = "entra";

    /// <summary>Attribute that carries the Entra object id. Used as the subject when the assertion includes it.</summary>
    public const string ObjectIdAttribute = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>The only attribute that counts as a verified email.</summary>
    public const string EmailAttribute = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

    /// <summary>Entra tenant id.</summary>
    public string? TenantId { get; set; }

    /// <summary>Application (client) id.</summary>
    public string? ApplicationId { get; set; }

    /// <summary>Service-provider entity id. Must match the Entra Identifier.</summary>
    public string? EntityId { get; set; }

    /// <summary>Assertion consumer URL. Must match the Entra Reply URL.</summary>
    public string? AcsUrl { get; set; }

    /// <summary>App federation metadata URL, including <c>?appid=</c>.</summary>
    public string MetadataUrl => $"https://login.microsoftonline.com/{TenantId}/federationmetadata/2007-06/federationmetadata.xml?appid={ApplicationId}";

    /// <summary>Issuer Entra puts on the assertion, including the trailing slash.</summary>
    public string ExpectedIssuer => $"https://sts.windows.net/{TenantId}/";

    /// <summary>Rejects a missing tenant, a missing application id, and an ACS URL that is not allowed.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(TenantId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ApplicationId);
        SamlServiceProviderRules.Validate(EntityId, AcsUrl);
    }
}