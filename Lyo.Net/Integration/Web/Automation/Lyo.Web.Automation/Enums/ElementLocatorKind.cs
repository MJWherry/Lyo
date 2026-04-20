namespace Lyo.Web.Automation.Enums;

/// <summary>How the locator value string is interpreted by the automation engine.</summary>
public enum ElementLocatorKind
{
    Id,
    Name,
    CssSelector,
    XPath,
    LinkText,
    PartialLinkText,
    ClassName,
    TagName
}
