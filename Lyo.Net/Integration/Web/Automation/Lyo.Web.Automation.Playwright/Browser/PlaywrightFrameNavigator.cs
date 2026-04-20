using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Web.Automation;
using Lyo.Web.Automation.Models;
using Wm = Lyo.Web.Automation.Constants;
using Lyo.Web.Automation.Playwright.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>Nested iframe targeting via Playwright <see cref="IFrameLocator" /> (selector stack).</summary>
public sealed class PlaywrightFrameNavigator
{
    private readonly PlaywrightBrowser _browser;
    private readonly List<string> _iframeSelectors = [];

    internal PlaywrightFrameNavigator(PlaywrightBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser, nameof(browser));
        _browser = browser;
    }

    /// <summary>Number of nested <see cref="SwitchToFrameAsync" /> levels from the root document.</summary>
    public int Depth => _iframeSelectors.Count;

    /// <summary>Switches into the frame that matches <paramref name="locator" /> (iframe element).</summary>
    public async Task SwitchToFrameAsync(ElementLocator locator, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        var sel = PlaywrightFrameSelectors.ToIframeSelector(locator);
        await RunFrameOpAsync(
            "switch_frame",
            async () => {
                var page = _browser.GetRequiredPage();
                await page.Locator(sel).WaitForAsync(
                        new LocatorWaitForOptions {
                            State = WaitForSelectorState.Attached,
                            Timeout = _browser.Options.LocatorDefaultTimeoutMs
                        })
                    .ConfigureAwait(false);
                _iframeSelectors.Add(sel);
            },
            ct).ConfigureAwait(false);
    }

    /// <summary>Synchronous wrapper for <see cref="SwitchToFrameAsync" />.</summary>
    public void SwitchToFrame(ElementLocator locator)
        => SwitchToFrameAsync(locator, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Moves one level up (parent frame).</summary>
    public void SwitchToParentFrame()
    {
        RunFrameOp(
            "parent_frame",
            () => {
                if (_iframeSelectors.Count <= 0)
                    throw new InvalidOperationException("Already at default content (no parent frame).");

                _iframeSelectors.RemoveAt(_iframeSelectors.Count - 1);
            });
    }

    /// <summary>Returns to the top-level document.</summary>
    public void SwitchToDefaultContent()
    {
        RunFrameOp("default_content", () => _iframeSelectors.Clear());
    }

    /// <summary>Enters a frame and returns a scope that calls <see cref="SwitchToParentFrame" /> on dispose.</summary>
    public async Task<PlaywrightFrameScope> EnterFrameAsync(ElementLocator locator, CancellationToken ct = default)
    {
        await SwitchToFrameAsync(locator, ct).ConfigureAwait(false);
        return new PlaywrightFrameScope(this);
    }

    /// <summary>Synchronous <see cref="EnterFrameAsync" />.</summary>
    public PlaywrightFrameScope EnterFrame(ElementLocator locator)
        => EnterFrameAsync(locator, CancellationToken.None).GetAwaiter().GetResult();

    internal ILocator ResolveLocator(ElementLocator elementLocator)
    {
        if (_iframeSelectors.Count == 0)
            return PlaywrightLocatorFactory.Locate(_browser.GetRequiredPage(), elementLocator);

        IFrameLocator cur = _browser.GetRequiredPage().FrameLocator(_iframeSelectors[0]);
        for (var i = 1; i < _iframeSelectors.Count; i++)
            cur = cur.FrameLocator(_iframeSelectors[i]);

        return PlaywrightLocatorFactory.Locate(cur, elementLocator);
    }

    private void RunFrameOp(string operation, Action action)
    {
        var log = _browser.Logger;
        log.LogDebug("Frame operation {Operation} starting", operation);
        try {
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), PlaywrightMetricTags.ForOperation(_browser, operation))) {
                action();
            }

            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.FrameOperation)),
                tags: PlaywrightMetricTags.ForOperation(_browser, operation, new[] { ("result", "success") }));
            log.LogDebug("Frame operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.FrameOperation)),
                tags: PlaywrightMetricTags.ForOperation(_browser, operation, new[] { ("result", "failure") }));
            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), ex, PlaywrightMetricTags.ForOperation(_browser, operation));
            log.LogWarning(ex, "Frame operation {Operation} failed", operation);
            throw;
        }
    }

    private async Task RunFrameOpAsync(string operation, Func<Task> action, CancellationToken ct)
    {
        var log = _browser.Logger;
        log.LogDebug("Frame operation {Operation} starting", operation);
        try {
            ct.ThrowIfCancellationRequested();
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), PlaywrightMetricTags.ForOperation(_browser, operation))) {
                await action().ConfigureAwait(false);
            }

            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.FrameOperation)),
                tags: PlaywrightMetricTags.ForOperation(_browser, operation, new[] { ("result", "success") }));
            log.LogDebug("Frame operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.FrameOperation)),
                tags: PlaywrightMetricTags.ForOperation(_browser, operation, new[] { ("result", "failure") }));
            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), ex, PlaywrightMetricTags.ForOperation(_browser, operation));
            log.LogWarning(ex, "Frame operation {Operation} failed", operation);
            throw;
        }
    }
}

/// <summary>Restores the parent frame when disposed (one <see cref="PlaywrightFrameNavigator.SwitchToParentFrame" />).</summary>
public sealed class PlaywrightFrameScope : IDisposable
{
    private readonly PlaywrightFrameNavigator _navigator;
    private int _disposed;

    internal PlaywrightFrameScope(PlaywrightFrameNavigator navigator)
    {
        _navigator = navigator;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _navigator.SwitchToParentFrame();
    }
}
