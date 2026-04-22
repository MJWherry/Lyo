using Lyo.Exceptions;
using Wm = Lyo.Web.Automation.Core.Constants;
using Lyo.Web.Automation.Selenium.Service;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>JavaScript alert / confirm / prompt handling (<see cref="ITargetLocator.Alert" />).</summary>
public sealed class BrowserAlerts
{
    private readonly SeleniumBrowser _browser;

    internal BrowserAlerts(SeleniumBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser, nameof(browser));
        _browser = browser;
    }

    /// <summary>Whether a JS dialog is currently displayed.</summary>
    public bool IsPresent => TryGetAlert(out _);

    /// <summary>Tries to switch to an alert, if any.</summary>
    public bool TryGetAlert(out IAlert? alert)
    {
        try {
            alert = _browser.GetRequiredDriver().SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException) {
            alert = null;
            return false;
        }
    }

    /// <summary>Gets the alert text, or null if none.</summary>
    public string? TryGetText()
    {
        if (!TryGetAlert(out var alert))
            return null;

        return alert!.Text;
    }

    /// <summary>Accepts the current alert (OK).</summary>
    public void Accept(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        RunAlert("accept", () => _browser.GetRequiredDriver().SwitchTo().Alert().Accept());
    }

    /// <summary>Dismisses the current alert (Cancel).</summary>
    public void Dismiss(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        RunAlert("dismiss", () => _browser.GetRequiredDriver().SwitchTo().Alert().Dismiss());
    }

    /// <summary>Sends keys to a prompt, then you typically <see cref="Accept" />.</summary>
    public void SendKeysToPrompt(string text, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(text, nameof(text));
        ct.ThrowIfCancellationRequested();
        RunAlert("prompt_send_keys", () => _browser.GetRequiredDriver().SwitchTo().Alert().SendKeys(text));
    }

    /// <summary>Accepts if an alert is present; returns whether an alert was handled.</summary>
    public bool TryAccept(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!TryGetAlert(out _))
            return false;

        RunAlert("try_accept", () => _browser.GetRequiredDriver().SwitchTo().Alert().Accept());
        return true;
    }

    /// <summary>Polls until an alert appears or the timeout elapses.</summary>
    public async Task WaitForAlertAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(_browser.Options.SeleniumMaxWaitSeconds);
        var deadline = DateTime.UtcNow + maxWait;
        while (DateTime.UtcNow < deadline) {
            ct.ThrowIfCancellationRequested();
            if (IsPresent)
                return;

            await Task.Delay(50, ct).ConfigureAwait(false);
        }

        throw new WebDriverTimeoutException("No JavaScript alert appeared within the wait period.");
    }

    private void RunAlert(string operation, Action action)
    {
        var log = _browser.Logger;
        log.LogDebug("Alert operation {Operation} starting", operation);
        try {
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.AlertOperationDuration)), SeleniumMetricTags.ForOperation(_browser, operation))) {
                action();
            }

            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.AlertOperation)),
                tags: SeleniumMetricTags.ForOperation(_browser, operation, [("result", "success")]));
            log.LogDebug("Alert operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.AlertOperation)),
                tags: SeleniumMetricTags.ForOperation(_browser, operation, [("result", "failure")]));
            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.AlertOperationDuration)), ex, SeleniumMetricTags.ForOperation(_browser, operation));
            log.LogWarning(ex, "Alert operation {Operation} failed", operation);
            throw;
        }
    }
}
