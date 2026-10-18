using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace Lyo.Authentication.Saml.Tests;

public enum SamlSignatureKind
{
    Assertion,
    Response,
    None,
    Sha1,
    InclusiveCanonicalization,
    CommentsCanonicalization
}

public sealed class SamlResponseSpec
{
    public string RequestId { get; set; } = "_req";

    public string ResponseId { get; set; } = "_resp";

    public string AssertionId { get; set; } = "_assert";

    public string Destination { get; set; } = SamlTestProvider.Acs;

    public string Recipient { get; set; } = SamlTestProvider.Acs;

    public string Issuer { get; set; } = SamlTestProvider.Issuer;

    public string Audience { get; set; } = SamlTestProvider.EntityId;

    public string NameId { get; set; } = "00uuser";

    public string NameIdFormat { get; set; } = "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified";

    public string Status { get; set; } = SamlConstants.StatusSuccess;

    public string ConfirmationMethod { get; set; } = SamlConstants.Bearer;

    public string EmailAttribute { get; set; } = "email";

    public string Email { get; set; } = "a@example.com";

    public bool IncludeInResponseTo { get; set; } = true;

    public bool IncludeAudience { get; set; } = true;

    public bool IncludeEncryptedAssertion { get; set; }

    public bool DuplicateAssertionId { get; set; }

    public bool SignDecoy { get; set; }

    public string? ReferenceUri { get; set; }

    public DateTime Now { get; set; } = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    public DateTime? ConditionsNotOnOrAfter { get; set; }

    public DateTime? ConfirmationNotOnOrAfter { get; set; }

    public SamlSignatureKind Signature { get; set; } = SamlSignatureKind.Assertion;

    public List<(string Name, string[] Values)> Attributes { get; } = [];

    public bool CorruptSignature { get; set; }

    public string ToXml()
    {
        var inResponseTo = IncludeInResponseTo ? $""" InResponseTo="{RequestId}" """ : " ";
        var audience = IncludeAudience
            ? $"""<saml:AudienceRestriction><saml:Audience>{Audience}</saml:Audience></saml:AudienceRestriction>"""
            : "";
        var encrypted = IncludeEncryptedAssertion ? "<saml:EncryptedAssertion><saml:EncryptedData/></saml:EncryptedAssertion>" : "";
        var duplicate = DuplicateAssertionId ? $"""<saml:Advice ID="{AssertionId}"/>""" : "";
        var attributes = new StringBuilder();
        if (Email.Length > 0)
            attributes.Append($"""<saml:Attribute Name="{EmailAttribute}"><saml:AttributeValue>{Email}</saml:AttributeValue></saml:Attribute>""");
        foreach (var (name, values) in Attributes)
        {
            attributes.Append($"""<saml:Attribute Name="{name}">""");
            foreach (var value in values)
                attributes.Append($"<saml:AttributeValue>{value}</saml:AttributeValue>");
            attributes.Append("</saml:Attribute>");
        }

        var decoy = SignDecoy ? """<saml:Advice ID="_decoy"><saml:NameID>decoy-user</saml:NameID></saml:Advice>""" : "";
        var xml = $"""
            <samlp:Response xmlns:samlp="{SamlConstants.Protocol}" xmlns:saml="{SamlConstants.Assertion}" ID="{ResponseId}" Version="2.0" IssueInstant="{Format(Now)}"{inResponseTo}Destination="{Destination}">
              <saml:Issuer>{Issuer}</saml:Issuer>
              <samlp:Status><samlp:StatusCode Value="{Status}"/></samlp:Status>
              {encrypted}
              <saml:Assertion ID="{AssertionId}" Version="2.0" IssueInstant="{Format(Now)}">
                <saml:Issuer>{Issuer}</saml:Issuer>
                <saml:Subject>
                  <saml:NameID Format="{NameIdFormat}">{NameId}</saml:NameID>
                  <saml:SubjectConfirmation Method="{ConfirmationMethod}">
                    <saml:SubjectConfirmationData InResponseTo="{RequestId}" Recipient="{Recipient}" NotOnOrAfter="{Format(ConfirmationNotOnOrAfter ?? Now.AddMinutes(5))}"/>
                  </saml:SubjectConfirmation>
                </saml:Subject>
                <saml:Conditions NotBefore="{Format(Now.AddMinutes(-1))}" NotOnOrAfter="{Format(ConditionsNotOnOrAfter ?? Now.AddMinutes(5))}">
                  {audience}
                </saml:Conditions>
                <saml:AttributeStatement>
                  {attributes}
                </saml:AttributeStatement>
                {decoy}
              </saml:Assertion>
              {duplicate}
            </samlp:Response>
            """;

        if (Signature == SamlSignatureKind.None && !SignDecoy)
            return xml;

        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(xml);
        if (SignDecoy)
            Sign(document, Element(document, "Advice", SamlConstants.Assertion), "#_decoy", SamlSignatureKind.Assertion);
        else if (Signature == SamlSignatureKind.Response)
            Sign(document, Element(document, "Response", SamlConstants.Protocol), ReferenceUri ?? "#" + ResponseId, Signature);
        else
            Sign(document, Element(document, "Assertion", SamlConstants.Assertion), ReferenceUri ?? "#" + AssertionId, Signature);

        if (CorruptSignature)
        {
            var value = document.GetElementsByTagName("SignatureValue", SamlConstants.XmlDsig)[0]!;
            var text = value.InnerText;
            value.InnerText = (text[0] == 'A' ? "B" : "A") + text[1..];
        }

        return document.OuterXml;
    }

    public string ToBase64() => Convert.ToBase64String(Encoding.UTF8.GetBytes(ToXml()));

    private static void Sign(XmlDocument document, XmlElement element, string referenceUri, SamlSignatureKind kind)
    {
        var signed = new IdSigningXml(document) { SigningKey = SamlTestCertificate.Signing.GetRSAPrivateKey() };
        signed.SignedInfo!.CanonicalizationMethod = kind == SamlSignatureKind.InclusiveCanonicalization
            ? SignedXml.XmlDsigC14NTransformUrl
            : kind == SamlSignatureKind.CommentsCanonicalization
                ? "http://www.w3.org/2001/10/xml-exc-c14n#WithComments"
                : SamlConstants.ExclusiveC14N;
        signed.SignedInfo.SignatureMethod = kind == SamlSignatureKind.Sha1 ? SignedXml.XmlDsigRSASHA1Url : SignedXml.XmlDsigRSASHA256Url;
        var reference = new Reference(referenceUri) { DigestMethod = kind == SamlSignatureKind.Sha1 ? SignedXml.XmlDsigSHA1Url : SignedXml.XmlDsigSHA256Url };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(kind switch
        {
            SamlSignatureKind.InclusiveCanonicalization => new XmlDsigC14NTransform(),
            SamlSignatureKind.CommentsCanonicalization => new XmlDsigExcC14NTransform(includeComments: true),
            _ => new XmlDsigExcC14NTransform()
        });
        signed.AddReference(reference);
        var info = new KeyInfo();
        info.AddClause(new KeyInfoX509Data(SamlTestCertificate.Signing));
        signed.KeyInfo = info;
        signed.ComputeSignature();
        element.AppendChild(document.ImportNode(signed.GetXml(), true));
    }

    private sealed class IdSigningXml(XmlDocument document) : SignedXml(document)
    {
        public override XmlElement? GetIdElement(XmlDocument? document, string idValue)
        {
            if (document?.DocumentElement is null)
                return null;
            var stack = new Stack<XmlNode>();
            stack.Push(document.DocumentElement);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node is XmlElement element && element.GetAttribute("ID") == idValue)
                    return element;
                foreach (XmlNode child in node.ChildNodes)
                    stack.Push(child);
            }

            return null;
        }
    }

    private static XmlElement Element(XmlDocument document, string localName, string namespaceUri)
        => document.GetElementsByTagName(localName, namespaceUri)[0] as XmlElement ?? throw new InvalidOperationException($"Missing {localName}.");

    private static string Format(DateTime value) => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
}

internal sealed record SamlTestProvider(string Name, string SpEntityId, string AcsUrl, string MetadataUrl, string EmailAttributeName, string? SubjectAttributeName) : ISamlIdentityProvider
{
    public static SamlTestProvider Default { get; } = new("okta", EntityId, Acs, "https://idp.example/metadata", "email", null);

    public const string EntityId = "https://sp.example/saml";

    public const string Acs = "https://sp.example/auth/callback/okta";

    public const string Issuer = "https://idp.example/metadata";

    public const string SsoRedirect = "https://idp.example/sso/redirect";
}

internal sealed class StaticSamlMetadata(SamlIdpMetadata metadata) : ISamlMetadataSource
{
    public Task<SamlIdpMetadata> GetAsync(ISamlIdentityProvider provider, CancellationToken ct = default) => Task.FromResult(metadata);
}
