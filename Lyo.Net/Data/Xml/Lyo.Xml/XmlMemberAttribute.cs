namespace Lyo.Xml;

/// <summary>
/// Writes a scalar property as an XML attribute of its parent element, or forces it to stay a child element. A property without this attribute follows
/// <see cref="XmlOptions.ScalarsAsAttributes" />.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class XmlMemberAttribute : Attribute
{
    /// <summary>When true, the property is an attribute. When false, it stays an element even if <see cref="XmlOptions.ScalarsAsAttributes" /> is on. Starts true.</summary>
    public bool AsAttribute { get; set; } = true;
}