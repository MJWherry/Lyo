using Lyo.Exceptions;
using Lyo.Web.Automation.Selenium.Controls;
using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Browser;

internal sealed class SeleniumWebAutomationElement : IWebAutomationElement
{
    private readonly SeleniumBrowser _browser;
    private readonly IWebElement _element;

    public SeleniumWebAutomationElement(SeleniumBrowser browser, IWebElement element)
    {
        _browser = browser;
        _element = element;
    }

    public Task ClickAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                _element.Click();
            }, ct);

    public Task SendKeysAsync(string text, bool clearFirst, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                if (clearFirst)
                    new InputControl(_element).SendKeys(text);
                else
                    _element.SendKeys(text);
            }, ct);
    }

    public Task SendKeysRawAsync(string keys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(keys, nameof(keys));
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                _element.SendKeys(keys);
            }, ct);
    }

    public Task ClearAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                _element.Clear();
            }, ct);

    public Task SubmitAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                _element.Submit();
            }, ct);

    public Task SelectByTextAsync(string text, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                new SelectControl(_element).SelectByText(text);
            }, ct);
    }

    public Task SelectByValueAsync(string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(value, nameof(value));
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                new SelectControl(_element).SelectByValue(value);
            }, ct);
    }

    public Task SelectByIndexAsync(int index, CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                new SelectControl(_element).SelectByIndex(index);
            }, ct);

    public Task ScrollIntoViewAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                _browser.ScrollIntoView(_element);
            }, ct);

    /// <inheritdoc />
    public Task<string?> GetAttributeAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return _element.GetAttribute(name);
            }, ct);
    }

    /// <inheritdoc />
    public Task<string> GetTextAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                return _element.Text;
            }, ct);

    /// <inheritdoc />
    public async Task<IWebAutomationElement> PollForDescendantAsync(ElementLocator locator, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        var el = await _browser.PollForDescendantElementAsync(_element, locator, ct).ConfigureAwait(false);
        return new SeleniumWebAutomationElement(_browser, el);
    }

    /// <inheritdoc />
    public Task<byte[]> TakeSnapshotPngAsync(CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                if (_element is not ITakesScreenshot shot)
                    throw new NotSupportedException("Element does not support screenshots.");

                return shot.GetScreenshot().AsByteArray;
            }, ct);
}