using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Controls;

/// <summary>Wrapper for button-like elements (button, input[type=submit], input[type=button], etc.).</summary>
public class ButtonControl : WebElementControl
{
    /// <summary>Creates a new button control wrapping the given element.</summary>
    internal ButtonControl(IWebElement element)
        : base(element) { }
}