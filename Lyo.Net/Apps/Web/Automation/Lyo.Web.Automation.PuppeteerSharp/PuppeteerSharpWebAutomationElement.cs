using Lyo.Exceptions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.PuppeteerSharp.Browser;
using PuppeteerSharp;

namespace Lyo.Web.Automation.PuppeteerSharp;

internal sealed class PuppeteerSharpWebAutomationElement(IElementHandle handle, PuppeteerSharpBrowser browser) : IWebAutomationElement
{
    public async Task ClickAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await handle.ClickAsync().ConfigureAwait(false);
    }

    public async Task SendKeysAsync(string text, bool clearFirst, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text);
        ct.ThrowIfCancellationRequested();
        if (clearFirst)
            await SetValueAsync(text).ConfigureAwait(false);
        else
            await handle.TypeAsync(text).ConfigureAwait(false);
    }

    public async Task SendKeysRawAsync(string keys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(keys);
        ct.ThrowIfCancellationRequested();
        await handle.TypeAsync(keys).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await SetValueAsync("").ConfigureAwait(false);
    }

    public async Task SubmitAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await handle.PressAsync("Enter").ConfigureAwait(false);
    }

    public async Task SelectByTextAsync(string text, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text);
        ct.ThrowIfCancellationRequested();
        var value = await handle.EvaluateFunctionAsync<string>(
                "(el, label) => { const opt = [...el.options].find(o => (o.textContent || '').trim() === label); return opt ? opt.value : ''; }", text)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(value) && text.Length > 0)
            throw new InvalidOperationException($"Select option text not found: {text}");

        await handle.SelectAsync(value).ConfigureAwait(false);
    }

    public async Task SelectByValueAsync(string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(value);
        ct.ThrowIfCancellationRequested();
        await handle.SelectAsync(value).ConfigureAwait(false);
    }

    public async Task SelectByIndexAsync(int index, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var value = await handle.EvaluateFunctionAsync<string>("(el, i) => { const opt = el.options[i]; return opt ? opt.value : ''; }", index).ConfigureAwait(false);
        await handle.SelectAsync(value).ConfigureAwait(false);
    }

    public async Task ScrollIntoViewAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await handle.EvaluateFunctionAsync("el => el.scrollIntoView({ block: 'nearest', inline: 'nearest' })").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetAttributeAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ct.ThrowIfCancellationRequested();
        return await handle.EvaluateFunctionAsync<string?>("(el, attr) => el.getAttribute(attr)", name).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetTextAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await handle.EvaluateFunctionAsync<string>("el => el.innerText").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetTagNameAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await handle.EvaluateFunctionAsync<string>("el => el.tagName.toLowerCase()").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IWebAutomationElement> PollForDescendantAsync(ElementLocator locator, CancellationToken ct = default) => browser.PollForDescendantElementAsync(handle, locator, ct);

    /// <inheritdoc />
    public async Task<byte[]> TakeSnapshotPngAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await handle.ScreenshotDataAsync().ConfigureAwait(false);
    }

    private Task SetValueAsync(string text)
        => handle.EvaluateFunctionAsync(
            "(el, value) => { el.focus(); el.value = value; el.dispatchEvent(new Event('input', { bubbles: true })); el.dispatchEvent(new Event('change', { bubbles: true })); }",
            text);
}