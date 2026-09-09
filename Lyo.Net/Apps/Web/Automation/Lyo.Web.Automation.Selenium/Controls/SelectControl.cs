using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Lyo.Web.Automation.Selenium.Controls;

/// <summary>Wrapper around select and dropdown elements.</summary>
public class SelectControl : WebElementControl
{
    private readonly SelectElement _select;

    /// <summary>Reads the selected option's text.</summary>
    public string SelectedOptionText => _select.SelectedOption.Text;

    /// <summary>Reads the selected option's value.</summary>
    public string SelectedOptionValue => _select.SelectedOption.GetAttribute("value") ?? string.Empty;

    /// <summary>Reads every option element.</summary>
    public IList<IWebElement> Options => _select.Options;

    /// <summary>Creates a new select control that wraps the given element.</summary>
    internal SelectControl(IWebElement element)
        : base(element)
        => _select = new(element);

    /// <summary>Selects an option by value.</summary>
    public void SelectByValue(string value) => _select.SelectByValue(value);

    /// <summary>Selects an option by visible text.</summary>
    public void SelectByText(string text) => _select.SelectByText(text);

    /// <summary>Selects an option by index (zero-based).</summary>
    public void SelectByIndex(int index) => _select.SelectByIndex(index);
}