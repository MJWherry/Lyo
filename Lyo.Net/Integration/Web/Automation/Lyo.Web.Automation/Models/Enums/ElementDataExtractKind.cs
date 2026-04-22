namespace Lyo.Web.Automation.Models.Enums;

/// <summary>What to read from an element when extracting into a variable.</summary>
public enum ElementDataExtractKind
{
    /// <summary>DOM attribute (requires attribute name on the extract step).</summary>
    Attribute,

    /// <summary>Visible text (same semantics as the automation element text accessor).</summary>
    Text
}
