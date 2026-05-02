using Lyo.Privacy.Xml;

namespace Lyo.Privacy.Enums;

/// <summary>How to redact text in a sensitive XML element.</summary>
public enum XmlScalarStrategy
{
    /// <summary>Replace text with <see cref="XmlRedactorOptions.Placeholder" />.</summary>
    Placeholder,

    /// <summary>Remove the element (and descendants) entirely.</summary>
    RemoveElement
}