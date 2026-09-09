using Lyo.Privacy.Xml;

namespace Lyo.Privacy.Enums;

/// <summary>How text in a sensitive XML element is redacted.</summary>
public enum XmlScalarStrategy
{
    /// <summary>Replace the text with <see cref="XmlRedactorOptions.Placeholder" />.</summary>
    Placeholder,

    /// <summary>Remove the element and its descendants entirely.</summary>
    RemoveElement
}