using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Controls;

/// <summary>Base wrapper for Selenium web elements with shared interaction methods.</summary>
public class WebElementControl
{
    /// <summary>Reads the underlying Selenium web element.</summary>
    public IWebElement Element { get; }

    /// <summary>Reads the visible text of the element.</summary>
    public string Text => Element.Text;

    /// <summary>Reads whether the element is displayed.</summary>
    public bool Displayed => Element.Displayed;

    /// <summary>Reads whether the element is enabled.</summary>
    public bool Enabled => Element.Enabled;

    /// <summary>Reads the tag name of the element.</summary>
    public string TagName => Element.TagName;

    /// <summary>Creates a new control that wraps the given element.</summary>
    internal WebElementControl(IWebElement element) => Element = element;

    /// <summary>Clicks this element.</summary>
    public virtual void Click() => Element.Click();

    /// <summary>Reads the value of the specified attribute.</summary>
    public string? GetAttribute(string name) => Element.GetAttribute(name);
}