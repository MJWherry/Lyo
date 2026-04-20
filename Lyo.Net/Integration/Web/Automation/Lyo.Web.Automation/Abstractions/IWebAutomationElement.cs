using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Abstractions;

/// <summary>Engine-neutral element handle for automation plans (implemented by Selenium, Playwright, etc.).</summary>
public interface IWebAutomationElement
{
    Task ClickAsync(CancellationToken ct = default);

    /// <summary>Clears (when supported), then sends text. When <paramref name="clearFirst" /> is false, appends without clearing.</summary>
    Task SendKeysAsync(string text, bool clearFirst, CancellationToken ct = default);

    Task SendKeysRawAsync(string keys, CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);

    Task SubmitAsync(CancellationToken ct = default);

    Task SelectByTextAsync(string text, CancellationToken ct = default);

    Task SelectByValueAsync(string value, CancellationToken ct = default);

    Task SelectByIndexAsync(int index, CancellationToken ct = default);

    Task ScrollIntoViewAsync(CancellationToken ct = default);

    /// <summary>DOM attribute value (e.g. <c>src</c>, <c>href</c>, <c>data-*</c>); <see langword="null" /> when absent.</summary>
    Task<string?> GetAttributeAsync(string name, CancellationToken ct = default);

    /// <summary>Visible text of the element (same idea as DOM inner text / Selenium element text).</summary>
    Task<string> GetTextAsync(CancellationToken ct = default);

    /// <summary>Polls for a descendant of this element matching <paramref name="locator" /> (scoped search).</summary>
    Task<IWebAutomationElement> PollForDescendantAsync(ElementLocator locator, CancellationToken ct = default);
}
