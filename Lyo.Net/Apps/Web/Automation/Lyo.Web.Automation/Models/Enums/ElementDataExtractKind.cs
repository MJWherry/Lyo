namespace Lyo.Web.Automation.Models.Enums;

/// <summary>What to read from an element when writing into a variable.</summary>
public enum ElementDataExtractKind
{
    /// <summary>DOM attribute (needs an attribute name on the extract step).</summary>
    Attribute,

    /// <summary>Visible text (same meaning as the automation element text accessor).</summary>
    Text
}