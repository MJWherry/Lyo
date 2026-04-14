using OpenQA.Selenium;

namespace Lyo.Scraping.Controls;

/// <summary>Wrapper for input elements (text, password, etc.).</summary>
public class InputControl : ScraperControl
{
    /// <summary>Gets or sets the value of the input.</summary>
    public string Value {
        get => Element.GetAttribute("value") ?? string.Empty;
        set => SendKeys(value);
    }

    /// <summary>Creates a new input control wrapping the given element.</summary>
    internal InputControl(IWebElement element)
        : base(element) { }

    /// <summary>Clears the input and sends the specified keys.</summary>
    public void SendKeys(string text)
    {
        Element.Clear();
        Element.SendKeys(text);
    }
}