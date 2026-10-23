using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Lyo.Authentication.Sso.Completion;
using Lyo.Exceptions;

namespace Lyo.Authentication.Saml;

/// <summary>Validates one SAML HTTP-POST response. Trust is a metadata signing certificate. The signature reference must be the single assertion, or the response that contains it.</summary>
public sealed class SamlResponseValidator
{
    /// <summary>Validates <paramref name="samlResponseBase64" /> and maps it to a login assertion. The ticket is already consumed.</summary>
    public ExternalLoginAssertion Validate(
        string samlResponseBase64,
        SamlLoginTicket ticket,
        ISamlIdentityProvider provider,
        SamlIdpMetadata metadata,
        DateTime utcNow,
        ISamlAssertionReplayCache replay)
    {
        ArgumentHelpers.ThrowIfNull(ticket);
        ArgumentHelpers.ThrowIfNull(provider);
        ArgumentHelpers.ThrowIfNull(metadata);
        ArgumentHelpers.ThrowIfNull(replay);
        var document = LoadResponse(samlResponseBase64);
        RejectEncrypted(document);
        var response = Single(document, "Response", SamlConstants.Protocol, "SamlResponse");
        var assertion = Single(document, "Assertion", SamlConstants.Assertion, "SamlResponse");
        RejectDuplicateIds(document);
        var notOnOrAfter = VerifySignatureAndReadTimes(document, response, assertion, provider, metadata, ticket, utcNow);
        if (!replay.TryRemember(assertion.GetAttribute("ID"), notOnOrAfter))
            throw new ExternalLoginRejectedException("SamlReplay", "This SAML assertion was already accepted.");

        return Map(assertion, provider);
    }

    private static XmlDocument LoadResponse(string samlResponseBase64)
    {
        byte[] decoded;
        try {
            decoded = Convert.FromBase64String(samlResponseBase64);
        }
        catch (FormatException) {
            throw new ExternalLoginRejectedException("SamlResponse", "SAMLResponse is not base64.");
        }

        if (decoded.Length == 0 || decoded.Length > SamlConstants.MaxDocumentBytes)
            throw new ExternalLoginRejectedException("SamlResponse", "SAMLResponse exceeds the size cap.");

        try {
            return SamlXml.Load(Encoding.UTF8.GetString(decoded));
        }
        catch (Exception ex) when (ex is not ExternalLoginRejectedException) {
            throw new ExternalLoginRejectedException("SamlResponse", "SAMLResponse is not a readable XML document.");
        }
    }

    private static void RejectEncrypted(XmlDocument document)
    {
        if (SamlXml.Elements(document, "EncryptedAssertion", SamlConstants.Assertion).Count > 0 || SamlXml.Elements(document, "EncryptedID", SamlConstants.Assertion).Count > 0)
            throw new ExternalLoginRejectedException("SamlEncrypted", "Encrypted SAML assertions are not accepted.");
    }

    private static XmlElement Single(XmlDocument document, string localName, string namespaceUri, string reason)
    {
        var elements = SamlXml.Elements(document, localName, namespaceUri);
        if (elements.Count != 1)
            throw new ExternalLoginRejectedException(reason, $"The SAML document must contain exactly one {localName}.");

        return elements[0];
    }

    private static void RejectDuplicateIds(XmlDocument document)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<XmlNode>();
        stack.Push(document);
        while (stack.Count > 0) {
            var node = stack.Pop();
            if (node is XmlElement element) {
                var id = element.GetAttribute("ID");
                if (id.Length > 0 && !seen.Add(id))
                    throw new ExternalLoginRejectedException("SamlSignature", "The SAML document repeats an ID.");
            }

            foreach (XmlNode child in node.ChildNodes)
                stack.Push(child);
        }
    }

    private static DateTime VerifySignatureAndReadTimes(
        XmlDocument document,
        XmlElement response,
        XmlElement assertion,
        ISamlIdentityProvider provider,
        SamlIdpMetadata metadata,
        SamlLoginTicket ticket,
        DateTime utcNow)
    {
        var signatures = SamlXml.Elements(document, "Signature", SamlConstants.XmlDsig);
        if (signatures.Count != 1)
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML document must contain exactly one signature.");

        var signature = signatures[0];
        var referenceUri = ReferenceUri(signature);
        var pinned = Pin(response, assertion, referenceUri);
        if (!IsInside(pinned, signature))
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature does not cover the assertion.");

        var signed = new PinnedSignedXml(document);
        signed.Pin(pinned);
        try {
            signed.LoadXml(signature);
        }
        catch (CryptographicException) {
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature cannot be read.");
        }

        RequireReference(signed, referenceUri);
        RequireAlgorithms(signed);
        if (!MatchesMetadata(signed, metadata))
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature does not match a metadata signing certificate.");

        return RequireConditions(response, assertion, provider, metadata, ticket, utcNow);
    }

    private static string ReferenceUri(XmlElement signature)
    {
        var references = SamlXml.Elements(signature, "Reference", SamlConstants.XmlDsig);
        if (references.Count != 1)
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature must have one reference.");

        var uri = references[0].GetAttribute("URI");
        if (string.IsNullOrEmpty(uri) || !uri.StartsWith('#'))
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature reference must point at an ID.");

        return uri;
    }

    private static void RequireReference(SignedXml signed, string referenceUri)
    {
        if (signed.SignedInfo is null || signed.SignedInfo.References.Count != 1 || signed.SignedInfo.References[0] is not Reference reference)
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature must have one reference.");

        var uri = reference.Uri;
        if (!string.Equals(uri, referenceUri, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature reference must point at an ID.");

        var sawEnveloped = false;
        foreach (Transform transform in reference.TransformChain) {
            if (transform.Algorithm == SignedXml.XmlDsigEnvelopedSignatureTransformUrl)
                sawEnveloped = true;
            else if (transform.Algorithm != SamlConstants.ExclusiveC14N)
                throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature uses a transform that is not allowed.");
        }

        if (!sawEnveloped)
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature is missing the enveloped-signature transform.");

        if (reference.DigestMethod is not ("http://www.w3.org/2001/04/xmlenc#sha256" or "http://www.w3.org/2001/04/xmlenc#sha384" or "http://www.w3.org/2001/04/xmlenc#sha512"))
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature digest must be SHA-256 or stronger.");
    }

    private static XmlElement Pin(XmlElement response, XmlElement assertion, string referenceUri)
    {
        var assertionId = assertion.GetAttribute("ID");
        var responseId = response.GetAttribute("ID");
        if (referenceUri == "#" + assertionId && assertionId.Length > 0)
            return assertion;

        if (referenceUri == "#" + responseId && responseId.Length > 0)
            return response;

        throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature reference does not match the assertion or the response.");
    }

    private static void RequireAlgorithms(SignedXml signed)
    {
        if (signed.SignedInfo?.CanonicalizationMethod != SamlConstants.ExclusiveC14N)
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature must use exclusive canonicalization without comments.");

        if (signed.SignatureMethod is not ("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256" or "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384"
            or "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512" or "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256" or "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha384"
            or "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha512"))
            throw new ExternalLoginRejectedException("SamlSignature", "The SAML signature algorithm must be SHA-256 or stronger.");
    }

    private static bool MatchesMetadata(PinnedSignedXml signed, SamlIdpMetadata metadata)
    {
        foreach (var der in metadata.SigningCertificates) {
            try {
                using var certificate = X509CertificateLoader.LoadCertificate(der);
                if (signed.CheckSignature(certificate, true))
                    return true;
            }
            catch (CryptographicException) {
                // Try the next rollover certificate.
            }
        }

        return false;
    }

    private static DateTime RequireConditions(
        XmlElement response,
        XmlElement assertion,
        ISamlIdentityProvider provider,
        SamlIdpMetadata metadata,
        SamlLoginTicket ticket,
        DateTime utcNow)
    {
        var status = SamlXml.Child(response, "Status", SamlConstants.Protocol);
        var code = status is null ? null : SamlXml.Child(status, "StatusCode", SamlConstants.Protocol);
        if (code?.GetAttribute("Value") != SamlConstants.StatusSuccess)
            throw new ExternalLoginRejectedException("SamlStatus", "The SAML response status is not Success.");

        var responseIssuer = SamlXml.Child(response, "Issuer", SamlConstants.Assertion)?.InnerText.Trim();
        var assertionIssuer = SamlXml.Child(assertion, "Issuer", SamlConstants.Assertion)?.InnerText.Trim();
        if (!string.Equals(responseIssuer, metadata.EntityId, StringComparison.Ordinal) || !string.Equals(assertionIssuer, metadata.EntityId, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("SamlIssuer", "The SAML issuer does not match this provider's metadata.");

        var inResponseTo = response.GetAttribute("InResponseTo");
        if (string.IsNullOrEmpty(inResponseTo))
            throw new ExternalLoginRejectedException("SamlInResponseTo", "The SAML response has no InResponseTo.");

        if (!string.Equals(inResponseTo, ticket.RequestId, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("SamlInResponseTo", "The SAML response is not for this login.");

        var restrictions = SamlXml.Elements(assertion, "AudienceRestriction", SamlConstants.Assertion);
        if (restrictions.Count == 0)
            throw new ExternalLoginRejectedException("SamlAudience", "The SAML assertion has no audience.");

        foreach (var restriction in restrictions) {
            var matched = SamlXml.Elements(restriction, "Audience", SamlConstants.Assertion)
                .Any(audience => string.Equals(audience.InnerText.Trim(), provider.SpEntityId, StringComparison.Ordinal));

            if (!matched)
                throw new ExternalLoginRejectedException("SamlAudience", "The SAML audience does not include this service provider.");
        }

        if (!string.Equals(response.GetAttribute("Destination"), provider.AcsUrl, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("SamlDestination", "The SAML destination does not match the assertion consumer URL.");

        var confirmation = SamlXml.Elements(assertion, "SubjectConfirmation", SamlConstants.Assertion)
            .FirstOrDefault(element => element.GetAttribute("Method") == SamlConstants.Bearer);

        if (confirmation is null)
            throw new ExternalLoginRejectedException("SamlConfirmation", "The SAML subject confirmation must be bearer.");

        var data = SamlXml.Child(confirmation, "SubjectConfirmationData", SamlConstants.Assertion) ??
            throw new ExternalLoginRejectedException("SamlConfirmation", "The SAML subject confirmation has no data.");

        if (!string.Equals(data.GetAttribute("Recipient"), provider.AcsUrl, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("SamlRecipient", "The SAML recipient does not match the assertion consumer URL.");

        if (!string.Equals(data.GetAttribute("InResponseTo"), ticket.RequestId, StringComparison.Ordinal))
            throw new ExternalLoginRejectedException("SamlInResponseTo", "The SAML subject confirmation is not for this login.");

        var confirmationExpiry = ParseInstant(data.GetAttribute("NotOnOrAfter")) ??
            throw new ExternalLoginRejectedException("SamlExpired", "The SAML subject confirmation has no NotOnOrAfter.");

        if (utcNow >= confirmationExpiry.Add(SamlConstants.ClockSkew))
            throw new ExternalLoginRejectedException("SamlExpired", "The SAML subject confirmation has expired.");

        var conditions = SamlXml.Child(assertion, "Conditions", SamlConstants.Assertion);
        if (conditions is not null) {
            var notBefore = ParseInstant(conditions.GetAttribute("NotBefore"));
            if (notBefore is not null && utcNow.Add(SamlConstants.ClockSkew) < notBefore.Value)
                throw new ExternalLoginRejectedException("SamlExpired", "The SAML assertion is not valid yet.");

            var conditionsExpiry = ParseInstant(conditions.GetAttribute("NotOnOrAfter"));
            if (conditionsExpiry is not null && utcNow >= conditionsExpiry.Value.Add(SamlConstants.ClockSkew))
                throw new ExternalLoginRejectedException("SamlExpired", "The SAML assertion has expired.");
        }

        return confirmationExpiry.Add(SamlConstants.ClockSkew);
    }

    private static ExternalLoginAssertion Map(XmlElement assertion, ISamlIdentityProvider provider)
    {
        var nameId = SamlXml.Elements(assertion, "NameID", SamlConstants.Assertion).FirstOrDefault();
        if (nameId is not null && nameId.GetAttribute("Format") == SamlConstants.TransientNameId)
            throw new ExternalLoginRejectedException("SamlTransientNameId", "Transient NameIDs are not accepted.");

        var claims = ReadAttributes(assertion);
        string subject;
        if (!string.IsNullOrWhiteSpace(provider.SubjectAttributeName) && claims.TryGetValue(provider.SubjectAttributeName, out var subjectClaim) &&
            First(subjectClaim) is { Length: > 0 } subjectValue)
            subject = subjectValue;
        else
            subject = nameId?.InnerText.Trim() ?? string.Empty;

        if (subject.Length == 0)
            throw new ExternalLoginRejectedException("SamlSubject", "The SAML assertion has no subject.");

        string? email = null;
        var emailVerified = false;
        if (claims.TryGetValue(provider.EmailAttributeName, out var emailClaim) && First(emailClaim) is { Length: > 0 } emailValue) {
            email = emailValue;
            emailVerified = true;
        }

        var displayName = claims.TryGetValue("displayName", out var display) ? First(display) : null;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = email ?? subject;

        return new(provider.Name, subject, displayName, email, emailVerified, null, null, [], claims);
    }

    private static Dictionary<string, object?> ReadAttributes(XmlElement assertion)
    {
        var claims = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var attribute in SamlXml.Elements(assertion, "Attribute", SamlConstants.Assertion)) {
            var name = attribute.GetAttribute("Name");
            if (name.Length == 0)
                continue;

            var values = SamlXml.Elements(attribute, "AttributeValue", SamlConstants.Assertion).Select(value => value.InnerText.Trim()).Where(value => value.Length > 0).ToArray();
            if (values.Length == 0)
                continue;

            claims[name] = values.Length == 1 ? values[0] : values;
        }

        return claims;
    }

    private static string? First(object? value)
        => value switch {
            string text => text,
            IReadOnlyList<string> list => list.FirstOrDefault(),
            var _ => null
        };

    private static DateTime? ParseInstant(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            throw new ExternalLoginRejectedException("SamlExpired", "A SAML time is not a valid instant.");

        return parsed.UtcDateTime;
    }

    private static bool IsInside(XmlElement ancestor, XmlElement node)
    {
        for (var current = node.ParentNode; current is not null; current = current.ParentNode) {
            if (ReferenceEquals(current, ancestor))
                return true;
        }

        return false;
    }

    private sealed class PinnedSignedXml(XmlDocument document)
        : SignedXml(document)
    {
        private XmlElement? _pinned;

        public void Pin(XmlElement element) => _pinned = element;

        public override XmlElement? GetIdElement(XmlDocument? document, string idValue)
        {
            if (_pinned is null)
                return null;

            return _pinned.GetAttribute("ID") == idValue ? _pinned : null;
        }
    }
}