using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Lyo.Authentication.Sso.Completion;
using Lyo.Exceptions;

namespace Lyo.Authentication.Saml;

/// <summary>Reads an IdP <c>EntityDescriptor</c>. Signing certificates come from <c>IDPSSODescriptor</c> only.</summary>
public static class SamlMetadataParser
{
    /// <summary>Parses metadata XML. Throws <see cref="ExternalLoginRejectedException" /> when the document is not a usable IdP descriptor.</summary>
    public static SamlIdpMetadata Parse(string xml)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(xml);
        XmlDocument document;
        try
        {
            document = SamlXml.Load(xml);
        }
        catch (Exception ex) when (ex is not ExternalLoginRejectedException)
        {
            throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata is not a readable XML document.");
        }

        var entity = SamlXml.Elements(document, "EntityDescriptor", SamlConstants.Metadata).FirstOrDefault()
            ?? throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata has no EntityDescriptor.");
        var entityId = entity.GetAttribute("entityID");
        if (string.IsNullOrWhiteSpace(entityId))
            throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata has no entityID.");

        var idp = SamlXml.Child(entity, "IDPSSODescriptor", SamlConstants.Metadata)
            ?? throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata has no IDPSSODescriptor.");

        var certificates = new List<byte[]>();
        foreach (var key in SamlXml.Elements(idp, "KeyDescriptor", SamlConstants.Metadata))
        {
            var use = key.GetAttribute("use");
            if (use.Equals("encryption", StringComparison.OrdinalIgnoreCase))
                continue;
            if (use.Length > 0 && !use.Equals("signing", StringComparison.OrdinalIgnoreCase))
                continue;

            var encoded = SamlXml.Elements(key, "X509Certificate", SamlConstants.XmlDsig).FirstOrDefault()?.InnerText;
            if (string.IsNullOrWhiteSpace(encoded))
                continue;

            try
            {
                var der = Convert.FromBase64String(encoded.Trim());
                _ = X509CertificateLoader.LoadCertificate(der);
                certificates.Add(der);
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata contains a signing certificate that cannot be read.");
            }
        }

        if (certificates.Count == 0)
            throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata has no signing certificate.");

        string? redirect = null;
        foreach (var service in SamlXml.Elements(idp, "SingleSignOnService", SamlConstants.Metadata))
        {
            if (!service.GetAttribute("Binding").Equals(SamlConstants.HttpRedirectBinding, StringComparison.Ordinal))
                continue;
            redirect = service.GetAttribute("Location");
            break;
        }

        if (string.IsNullOrWhiteSpace(redirect))
            throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata has no HTTP-Redirect SingleSignOnService.");

        return new SamlIdpMetadata(entityId, redirect, certificates);
    }
}
