using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Lyo.Authentication.Saml;

/// <summary>Builds an unsigned AuthnRequest and the HTTP-Redirect query value.</summary>
public static class SamlAuthnRequest
{
    /// <summary>Writes an AuthnRequest whose destination is the HTTP-Redirect sign-on URL.</summary>
    public static string Write(string id, DateTime issueInstant, string issuer, string destination, string acsUrl)
    {
        using var buffer = new StringWriter();
        var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Encoding = Encoding.UTF8 };
        using (var writer = XmlWriter.Create(buffer, settings))
        {
            writer.WriteStartElement("samlp", "AuthnRequest", SamlConstants.Protocol);
            writer.WriteAttributeString("xmlns", "saml", null, SamlConstants.Assertion);
            writer.WriteAttributeString("ID", id);
            writer.WriteAttributeString("Version", "2.0");
            writer.WriteAttributeString("IssueInstant", issueInstant.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            writer.WriteAttributeString("Destination", destination);
            writer.WriteAttributeString("AssertionConsumerServiceURL", acsUrl);
            writer.WriteAttributeString("ProtocolBinding", SamlConstants.HttpPostBinding);
            writer.WriteStartElement("saml", "Issuer", SamlConstants.Assertion);
            writer.WriteString(issuer);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        return buffer.ToString();
    }

    /// <summary>Raw DEFLATE (RFC 1951), then standard base64. Not a zlib wrapper.</summary>
    public static string Deflate(string xml)
    {
        var bytes = Encoding.UTF8.GetBytes(xml);
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            deflate.Write(bytes);
        return Convert.ToBase64String(output.ToArray());
    }

    /// <summary>Redirect URL with <c>SAMLRequest</c> and <c>RelayState</c>.</summary>
    public static string RedirectUrl(string ssoUrl, string deflatedRequest, string relayState)
    {
        var separator = ssoUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{ssoUrl}{separator}SAMLRequest={Uri.EscapeDataString(deflatedRequest)}&RelayState={Uri.EscapeDataString(relayState)}";
    }
}
