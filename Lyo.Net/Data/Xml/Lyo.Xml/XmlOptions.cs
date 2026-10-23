using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Json;

namespace Lyo.Xml;

/// <summary>
/// Options for <see cref="XmlService" />. The JSON contract fields control names, numbers, and omission. <see cref="JsonOptions.ReadNumbersFromStrings" /> starts true so
/// element text such as <c>36</c> stays a number.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class XmlOptions : JsonOptions
{
    /// <summary>Configuration section name. Nothing in this package binds it.</summary>
    public new const string SectionName = "Lyo:Xml";

    private readonly Dictionary<string, bool?> _named = new(StringComparer.Ordinal);

    private readonly Dictionary<(Type Type, string Name), bool?> _typed = [];

    /// <summary>Root element name. Empty uses the camelCase CLR name, or <c>items</c> for an array.</summary>
    public string RootElementName { get; set; } = "";

    /// <summary>Element name for a top-level array item. Starts as <c>item</c>. Repeated property elements keep the property name.</summary>
    public string ItemElementName { get; set; } = "item";

    /// <summary>When true, the writer omits the XML declaration. Starts true, which matches <see cref="System.Xml.Linq.XDocument" /> text.</summary>
    public bool OmitXmlDeclaration { get; set; } = true;

    /// <summary>Namespace for the root and its children. Empty means no namespace.</summary>
    public string Namespace { get; set; } = "";

    /// <summary>When true, scalar properties are attributes. Starts false, so values stay child elements and JSON types still round-trip.</summary>
    public bool ScalarsAsAttributes { get; set; }

    /// <summary>When true, an element whose text is empty is skipped. Nulls are already omitted by <see cref="JsonOptions.IgnoreNulls" />. Starts false.</summary>
    public bool OmitEmptyElements { get; set; }

    internal IReadOnlyDictionary<(Type Type, string Name), bool?> TypedMembers => _typed;

    internal IReadOnlyDictionary<string, bool?> NamedMembers => _named;

    /// <summary>Builds options. XML starts indented, and element text that is a canonical number is read as a number.</summary>
    public XmlOptions()
    {
        WriteIndented = true;
        ReadNumbersFromStrings = true;
    }

    /// <summary>Marks a CLR property as an attribute or an element. This overrides <see cref="XmlMemberAttribute" /> on that property.</summary>
    public void Member<T>(string propertyName, bool? asAttribute = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(propertyName);
        _typed[(typeof(T), propertyName)] = asAttribute;
    }

    /// <summary>Marks a written XML local name as an attribute or an element. Used when the document has no CLR type, including <c>ConvertFromJson</c>.</summary>
    public void Member(string localName, bool? asAttribute = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(localName);
        _named[localName] = asAttribute;
    }

    /// <summary>Root and item element names, plus the JSON contract limits.</summary>
    public override string ToString() => $"{base.ToString()} RootElementName={RootElementName} ItemElementName={ItemElementName} Namespace={Namespace}";
}