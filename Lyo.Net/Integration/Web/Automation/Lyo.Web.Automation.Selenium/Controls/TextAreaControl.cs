using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Controls;

/// <summary>Wrapper for textarea elements.</summary>
public sealed class TextAreaControl : WebElementControl
{
    /// <summary>Gets or sets the value of the textarea.</summary>
    public string Value {
        get => Element.GetAttribute("value") ?? string.Empty;
        set => SendKeys(value);
    }

    internal TextAreaControl(IWebElement element)
        : base(element) { }

    /// <summary>Clears the textarea and sends the specified keys.</summary>
    public void SendKeys(string text)
    {
        Element.Clear();
        Element.SendKeys(text);
    }
}