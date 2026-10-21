namespace Lyo.Authentication.Saml;

/// <summary>SAML 2.0 namespace and status URIs used by the service provider.</summary>
public static class SamlConstants
{
    /// <summary>SAML protocol namespace.</summary>
    public const string Protocol = "urn:oasis:names:tc:SAML:2.0:protocol";

    /// <summary>SAML assertion namespace.</summary>
    public const string Assertion = "urn:oasis:names:tc:SAML:2.0:assertion";

    /// <summary>SAML metadata namespace.</summary>
    public const string Metadata = "urn:oasis:names:tc:SAML:2.0:metadata";

    /// <summary>XML signature namespace.</summary>
    public const string XmlDsig = "http://www.w3.org/2000/09/xmldsig#";

    /// <summary>Successful status code.</summary>
    public const string StatusSuccess = "urn:oasis:names:tc:SAML:2.0:status:Success";

    /// <summary>Bearer subject confirmation.</summary>
    public const string Bearer = "urn:oasis:names:tc:SAML:2.0:cm:bearer";

    /// <summary>Transient NameID format. Rejected because the subject changes every login.</summary>
    public const string TransientNameId = "urn:oasis:names:tc:SAML:2.0:nameid-format:transient";

    /// <summary>HTTP-POST binding.</summary>
    public const string HttpPostBinding = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST";

    /// <summary>HTTP-Redirect binding.</summary>
    public const string HttpRedirectBinding = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect";

    /// <summary>Exclusive canonicalization without comments.</summary>
    public const string ExclusiveC14N = "http://www.w3.org/2001/10/xml-exc-c14n#";

    /// <summary>Maximum decoded SAML or metadata document size.</summary>
    public const int MaxDocumentBytes = 1_048_576;

    /// <summary>How long a login ticket and an AuthnRequest stay valid.</summary>
    public static readonly TimeSpan TicketTtl = TimeSpan.FromMinutes(15);

    /// <summary>Clock skew applied to NotBefore and NotOnOrAfter.</summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(2);
}