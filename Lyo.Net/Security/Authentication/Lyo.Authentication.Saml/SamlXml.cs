using System.IO;
using System.Xml;

namespace Lyo.Authentication.Saml;

/// <summary>Loads SAML XML with DTDs, external entities, and oversized documents refused.</summary>
public static class SamlXml
{
    /// <summary>Parses <paramref name="xml" /> with whitespace preserved and entity expansion disabled.</summary>
    public static XmlDocument Load(string xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = SamlConstants.MaxDocumentBytes
        };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.Load(reader);
        return document;
    }

    /// <summary>Elements in <paramref name="namespaceUri" /> with local name <paramref name="localName" />.</summary>
    public static List<XmlElement> Elements(XmlNode root, string localName, string namespaceUri)
    {
        var found = new List<XmlElement>();
        if (root is not XmlElement && root is not XmlDocument)
            return found;

        var nodes = root is XmlDocument document
            ? document.GetElementsByTagName(localName, namespaceUri)
            : ((XmlElement)root).GetElementsByTagName(localName, namespaceUri);
        foreach (XmlNode node in nodes)
        {
            if (node is XmlElement element)
                found.Add(element);
        }

        return found;
    }

    /// <summary>Direct child element, or null.</summary>
    public static XmlElement? Child(XmlElement parent, string localName, string namespaceUri)
    {
        foreach (XmlNode node in parent.ChildNodes)
        {
            if (node is XmlElement element && element.LocalName == localName && element.NamespaceURI == namespaceUri)
                return element;
        }

        return null;
    }
}
