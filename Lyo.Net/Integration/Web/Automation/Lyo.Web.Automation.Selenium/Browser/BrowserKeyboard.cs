using Lyo.Exceptions;
using Wm = Lyo.Web.Automation.Constants;
using Lyo.Web.Automation.Selenium.Service;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>Keyboard input via Selenium <see cref="Actions" /> (chords, focus, etc.).</summary>
public sealed class BrowserKeyboard
{
    private readonly LyoBrowser _browser;

    internal BrowserKeyboard(LyoBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser, nameof(browser));
        _browser = browser;
    }

    /// <summary>Sends keys to the active element (no specific target).</summary>
    public void SendKeys(string keys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(keys, nameof(keys));
        ct.ThrowIfCancellationRequested();
        RunKb("send_keys", () => new Actions(_browser.GetRequiredDriver()).SendKeys(keys).Perform());
    }

    /// <summary>Sends keys to a specific element (clicks focus first).</summary>
    public void SendKeys(IWebElement target, string keys, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(target, nameof(target));
        ArgumentHelpers.ThrowIfNull(keys, nameof(keys));
        ct.ThrowIfCancellationRequested();
        RunKb("send_keys_element", () => new Actions(_browser.GetRequiredDriver()).SendKeys(target, keys).Perform());
    }

    /// <summary>Performs a custom <see cref="Actions" /> sequence.</summary>
    public void Perform(Action<Actions> configure, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        ct.ThrowIfCancellationRequested();
        RunKb("actions_perform", () => {
            var actions = new Actions(_browser.GetRequiredDriver());
            configure(actions);
            actions.Perform();
        });
    }

    private void RunKb(string operation, Action action)
    {
        var log = _browser.Logger;
        log.LogDebug("Keyboard operation {Operation} starting", operation);
        try {
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.KeyboardOperationDuration)), SeleniumMetricTags.ForOperation(_browser, operation))) {
                action();
            }

            log.LogDebug("Keyboard operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.KeyboardOperationDuration)), ex, SeleniumMetricTags.ForOperation(_browser, operation));
            log.LogWarning(ex, "Keyboard operation {Operation} failed", operation);
            throw;
        }
    }
}
