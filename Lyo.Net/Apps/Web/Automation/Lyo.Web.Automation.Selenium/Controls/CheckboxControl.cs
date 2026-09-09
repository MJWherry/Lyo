using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Controls;

/// <summary>Wrapper around checkbox inputs.</summary>
public class CheckboxControl : WebElementControl
{
    /// <summary>Whether the checkbox is selected or checked.</summary>
    public bool Selected => Element.Selected;

    internal CheckboxControl(IWebElement element)
        : base(element) { }

    /// <summary>Selects the checkbox when it is not already selected.</summary>
    public void EnsureChecked()
    {
        if (!Selected)
            Click();
    }

    /// <summary>Clears the checkbox when it is selected.</summary>
    public void EnsureUnchecked()
    {
        if (Selected)
            Click();
    }
}