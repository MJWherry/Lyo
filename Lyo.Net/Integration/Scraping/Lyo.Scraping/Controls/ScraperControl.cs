using OpenQA.Selenium;

namespace Lyo.Scraping.Controls;

/// <summary>Base wrapper for Selenium web elements with common interaction methods.</summary>
public class ScraperControl
{
    /// <summary>Gets the underlying Selenium web element.</summary>
    public IWebElement Element { get; }

    /// <summary>Gets the visible text of the element.</summary>
    public string Text => Element.Text;

    /// <summary>Gets whether the element is displayed.</summary>
    public bool Displayed => Element.Displayed;

    /// <summary>Gets whether the element is enabled.</summary>
    public bool Enabled => Element.Enabled;

    /// <summary>Gets the tag name of the element.</summary>
    public string TagName => Element.TagName;

    /// <summary>Creates a new scraper control wrapping the given element.</summary>
    internal ScraperControl(IWebElement element) => Element = element;

    /// <summary>Clicks the element.</summary>
    public virtual void Click() => Element.Click();

    /// <summary>Gets the value of the specified attribute.</summary>
    public string? GetAttribute(string name) => Element.GetAttribute(name);
}