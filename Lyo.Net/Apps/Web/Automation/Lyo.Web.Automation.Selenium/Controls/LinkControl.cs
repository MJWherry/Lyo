using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Controls;

/// <summary>Wrapper around anchor and link elements.</summary>
public class LinkControl : WebElementControl
{
    /// <summary>Reads the href attribute.</summary>
    public string Href => Element.GetAttribute("href") ?? string.Empty;

    internal LinkControl(IWebElement element)
        : base(element) { }
}