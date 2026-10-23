using Lyo.Exceptions;
using Lyo.Web.Automation.PuppeteerSharp.Service;
using Microsoft.Extensions.Logging;
using Wm = Lyo.Web.Automation.Core.Constants;

namespace Lyo.Web.Automation.PuppeteerSharp.Browser;

/// <summary>Keyboard input via the active page.</summary>
public sealed class PuppeteerSharpKeyboard
{
    private readonly PuppeteerSharpBrowser _browser;

    internal PuppeteerSharpKeyboard(PuppeteerSharpBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        _browser = browser;
    }

    /// <summary>Presses a single key or named key (for example <c>Enter</c>, <c>Tab</c>).</summary>
    public async Task PressAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ct.ThrowIfCancellationRequested();
        await RunKbAsync("press", () => _browser.GetRequiredPage().Keyboard.PressAsync(key), ct).ConfigureAwait(false);
    }

    /// <summary>Types a string of characters.</summary>
    public async Task TypeAsync(string text, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text);
        ct.ThrowIfCancellationRequested();
        await RunKbAsync("type", () => _browser.GetRequiredPage().Keyboard.TypeAsync(text), ct).ConfigureAwait(false);
    }

    private async Task RunKbAsync(string operation, Func<Task> action, CancellationToken ct)
    {
        var log = _browser.Logger;
        log.LogDebug("Keyboard operation {Operation} starting", operation);
        try {
            ct.ThrowIfCancellationRequested();
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.KeyboardOperationDuration)), PuppeteerSharpMetricTags.ForOperation(_browser, operation)))
                await action().ConfigureAwait(false);

            log.LogDebug("Keyboard operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.KeyboardOperationDuration)), ex, PuppeteerSharpMetricTags.ForOperation(_browser, operation));
            log.LogWarning(ex, "Keyboard operation {Operation} failed", operation);
            throw;
        }
    }
}