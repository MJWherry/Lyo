using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Lyo.Scraping.Controls;

/// <summary>Wrapper for select/dropdown elements.</summary>
public class SelectControl : ScraperControl
{
    private readonly SelectElement _select;

    /// <summary>Gets the selected option's text.</summary>
    public string SelectedOptionText => _select.SelectedOption.Text;

    /// <summary>Gets the selected option's value.</summary>
    public string SelectedOptionValue => _select.SelectedOption.GetAttribute("value") ?? string.Empty;

    /// <summary>Gets all option elements.</summary>
    public IList<IWebElement> Options => _select.Options;

    /// <summary>Creates a new select control wrapping the given element.</summary>
    internal SelectControl(IWebElement element)
        : base(element)
        => _select = new(element);

    /// <summary>Selects the option by value.</summary>
    public void SelectByValue(string value) => _select.SelectByValue(value);

    /// <summary>Selects the option by visible text.</summary>
    public void SelectByText(string text) => _select.SelectByText(text);

    /// <summary>Selects the option by index (0-based).</summary>
    public void SelectByIndex(int index) => _select.SelectByIndex(index);
}