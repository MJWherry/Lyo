using Lyo.Exceptions;
using Lyo.Metrics;
using Wm = Lyo.Web.Automation.Constants;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Keyboard input via Playwright <see cref="IKeyboard" />.</summary>
public sealed class PlaywrightKeyboard
{
    private readonly PlaywrightBrowser _browser;

    internal PlaywrightKeyboard(PlaywrightBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser, nameof(browser));
        _browser = browser;
    }

    /// <summary>Presses a single key or named key (e.g. <c>Enter</c>, <c>Tab</c>).</summary>
    public async Task PressAsync(string key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ct.ThrowIfCancellationRequested();
        await RunKbAsync(
            "press",
            async () => {
                await _browser.GetRequiredPage().Keyboard.PressAsync(key).ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    /// <summary>Types a string of characters.</summary>
    public async Task TypeAsync(string text, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        ct.ThrowIfCancellationRequested();
        await RunKbAsync(
            "type",
            async () => {
                await _browser.GetRequiredPage().Keyboard.TypeAsync(text).ConfigureAwait(false);
            },
            ct).ConfigureAwait(false);
    }

    private async Task RunKbAsync(string operation, Func<Task> action, CancellationToken ct)
    {
        var log = _browser.Logger;
        log.LogDebug("Keyboard operation {Operation} starting", operation);
        try {
            ct.ThrowIfCancellationRequested();
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.KeyboardOperationDuration)), PlaywrightMetricTags.ForOperation(_browser, operation))) {
                await action().ConfigureAwait(false);
            }

            log.LogDebug("Keyboard operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.KeyboardOperationDuration)), ex, PlaywrightMetricTags.ForOperation(_browser, operation));
            log.LogWarning(ex, "Keyboard operation {Operation} failed", operation);
            throw;
        }
    }
}
