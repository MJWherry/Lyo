using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Playwright.Browser;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright;

internal sealed class PlaywrightWebAutomationElement(ILocator locator, PlaywrightBrowser browser) : IWebAutomationElement
{
    public async Task ClickAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await locator.ClickAsync().ConfigureAwait(false);
    }

    public async Task SendKeysAsync(string text, bool clearFirst, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        ct.ThrowIfCancellationRequested();
        if (clearFirst)
            await locator.FillAsync(text).ConfigureAwait(false);
        else
            await locator.PressSequentiallyAsync(text).ConfigureAwait(false);
    }

    public async Task SendKeysRawAsync(string keys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(keys, nameof(keys));
        ct.ThrowIfCancellationRequested();
        await locator.PressSequentiallyAsync(keys).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await locator.ClearAsync().ConfigureAwait(false);
    }

    public async Task SubmitAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await locator.PressAsync("Enter").ConfigureAwait(false);
    }

    public async Task SelectByTextAsync(string text, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        ct.ThrowIfCancellationRequested();
        await locator.SelectOptionAsync(new SelectOptionValue { Label = text }).ConfigureAwait(false);
    }

    public async Task SelectByValueAsync(string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(value, nameof(value));
        ct.ThrowIfCancellationRequested();
        await locator.SelectOptionAsync(new SelectOptionValue { Value = value }).ConfigureAwait(false);
    }

    public async Task SelectByIndexAsync(int index, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await locator.SelectOptionAsync(new SelectOptionValue { Index = index }).ConfigureAwait(false);
    }

    public async Task ScrollIntoViewAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await locator.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetAttributeAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ct.ThrowIfCancellationRequested();
        return await locator.GetAttributeAsync(name).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetTextAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var t = await locator.InnerTextAsync().ConfigureAwait(false);
        return t ?? string.Empty;
    }

    /// <inheritdoc />
    public Task<IWebAutomationElement> PollForDescendantAsync(ElementLocator locator1, CancellationToken ct = default)
        => browser.PollForDescendantElementAsync(locator, locator1, ct);
}
