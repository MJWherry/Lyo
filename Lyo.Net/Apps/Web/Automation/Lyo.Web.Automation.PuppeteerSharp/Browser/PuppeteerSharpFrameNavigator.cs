using Lyo.Exceptions;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.PuppeteerSharp.Service;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using Wm = Lyo.Web.Automation.Core.Constants;

namespace Lyo.Web.Automation.PuppeteerSharp.Browser;

/// <summary>Nested iframe targeting. The selector stack is re-resolved against the active page on each find.</summary>
public sealed class PuppeteerSharpFrameNavigator
{
    private readonly PuppeteerSharpBrowser _browser;
    private readonly List<string> _iframeSelectors = [];

    /// <summary>Number of nested <see cref="SwitchToFrameAsync" /> levels from the root document.</summary>
    public int Depth => _iframeSelectors.Count;

    internal PuppeteerSharpFrameNavigator(PuppeteerSharpBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        _browser = browser;
    }

    /// <summary>Switches into the frame that matches <paramref name="locator" /> (iframe element).</summary>
    public async Task SwitchToFrameAsync(ElementLocator locator, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(locator);
        var sel = PuppeteerSharpFrameSelectors.ToIframeSelector(locator);
        await RunFrameOpAsync(
                "switch_frame", async () => {
                    var frame = await CurrentFrameAsync(ct).ConfigureAwait(false);
                    var handle = await frame.WaitForSelectorAsync(sel, Wait(ct)).ConfigureAwait(false);
                    if (handle == null || await handle.ContentFrameAsync().ConfigureAwait(false) == null)
                        throw new InvalidOperationException($"Frame not found: {sel}");

                    _iframeSelectors.Add(sel);
                }, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Moves one level up (parent frame).</summary>
    public void SwitchToParentFrame()
        => RunFrameOp(
            "parent_frame", () => {
                OperationHelpers.ThrowIf(_iframeSelectors.Count <= 0, "Already at default content (no parent frame).");
                _iframeSelectors.RemoveAt(_iframeSelectors.Count - 1);
            });

    /// <summary>Yields to the top-level document.</summary>
    public void SwitchToDefaultContent() => RunFrameOp("default_content", () => _iframeSelectors.Clear());

    /// <summary>Enters a frame and yields a scope that calls <see cref="SwitchToParentFrame" /> on dispose.</summary>
    public async Task<PuppeteerSharpFrameScope> EnterFrameAsync(ElementLocator locator, CancellationToken ct = default)
    {
        await SwitchToFrameAsync(locator, ct).ConfigureAwait(false);
        return new(this);
    }

    internal async Task<IFrame> CurrentFrameAsync(CancellationToken ct)
    {
        IFrame frame = _browser.GetRequiredPage().MainFrame;
        foreach (var sel in _iframeSelectors) {
            ct.ThrowIfCancellationRequested();
            var handle = await frame.WaitForSelectorAsync(sel, Wait(ct)).ConfigureAwait(false);
            if (handle == null)
                throw new InvalidOperationException($"Frame not found: {sel}");

            var child = await handle.ContentFrameAsync().ConfigureAwait(false);
            if (child == null)
                throw new InvalidOperationException($"Element is not a frame: {sel}");

            frame = child;
        }

        return frame;
    }

    private WaitForSelectorOptions Wait(CancellationToken ct)
        => new() { Timeout = _browser.LocatorTimeoutMs, CancellationToken = ct };

    private void RunFrameOp(string operation, Action action)
    {
        var log = _browser.Logger;
        log.LogDebug("Frame operation {Operation} starting", operation);
        try {
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), PuppeteerSharpMetricTags.ForOperation(_browser, operation)))
                action();

            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.FrameOperation)), tags: PuppeteerSharpMetricTags.ForOperation(_browser, operation, [("result", "success")]));

            log.LogDebug("Frame operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.FrameOperation)), tags: PuppeteerSharpMetricTags.ForOperation(_browser, operation, [("result", "failure")]));

            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), ex, PuppeteerSharpMetricTags.ForOperation(_browser, operation));
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
            using (_browser.Metrics.StartTimer(_browser.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), PuppeteerSharpMetricTags.ForOperation(_browser, operation)))
                await action().ConfigureAwait(false);

            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.FrameOperation)), tags: PuppeteerSharpMetricTags.ForOperation(_browser, operation, [("result", "success")]));

            log.LogDebug("Frame operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _browser.Metrics.IncrementCounter(
                _browser.ResolveMetric(nameof(Wm.Metrics.FrameOperation)), tags: PuppeteerSharpMetricTags.ForOperation(_browser, operation, [("result", "failure")]));

            _browser.Metrics.RecordError(_browser.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), ex, PuppeteerSharpMetricTags.ForOperation(_browser, operation));
            log.LogWarning(ex, "Frame operation {Operation} failed", operation);
            throw;
        }
    }
}

/// <summary>Restores the parent frame when disposed.</summary>
public sealed class PuppeteerSharpFrameScope : IDisposable
{
    private readonly PuppeteerSharpFrameNavigator _navigator;
    private int _disposed;

    internal PuppeteerSharpFrameScope(PuppeteerSharpFrameNavigator navigator) => _navigator = navigator;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _navigator.SwitchToParentFrame();
    }
}
