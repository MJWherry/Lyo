using Lyo.Exceptions;
using Wm = Lyo.Web.Automation.Core.Constants;
using Lyo.Web.Automation.Selenium.Service;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace Lyo.Web.Automation.Selenium.Browser;

/// <summary>IFrame / nested browsing context switching with optional stack via <see cref="EnterFrame" /> scopes.</summary>
public sealed class FrameNavigator
{
    private readonly SeleniumBrowser _scraper;
    private int _depth;

    internal FrameNavigator(SeleniumBrowser scraper)
    {
        ArgumentHelpers.ThrowIfNull(scraper, nameof(scraper));
        _scraper = scraper;
    }

    /// <summary>Number of nested <see cref="SwitchToFrame(OpenQA.Selenium.IWebElement)" /> levels from default content.</summary>
    public int Depth => _depth;

    /// <summary>Switches to the frame located by <paramref name="by" /> (must be visible).</summary>
    public void SwitchToFrame(By by)
    {
        ArgumentHelpers.ThrowIfNull(by, nameof(by));
        var el = _scraper.WaitFor(by);
        OperationHelpers.ThrowIfNull(el, $"Frame not found: {by}");
        SwitchToFrame(el);
    }

    /// <summary>Switches to the given frame element.</summary>
    public void SwitchToFrame(IWebElement frame)
    {
        ArgumentHelpers.ThrowIfNull(frame, nameof(frame));
        RunFrameOp("switch_frame", () => {
            _scraper.GetRequiredDriver().SwitchTo().Frame(frame);
            _depth++;
        });
    }

    /// <summary>Moves one level up (parent frame), or throws if already at default content for this navigator.</summary>
    public void SwitchToParentFrame()
    {
        RunFrameOp("parent_frame", () => {
            if (_depth <= 0)
                throw new InvalidOperationException("Already at default content (no parent frame).");

            _scraper.GetRequiredDriver().SwitchTo().ParentFrame();
            _depth--;
        });
    }

    /// <summary>Returns to the top-level document (clears depth).</summary>
    public void SwitchToDefaultContent()
    {
        RunFrameOp("default_content", () => {
            _scraper.GetRequiredDriver().SwitchTo().DefaultContent();
            _depth = 0;
        });
    }

    /// <summary>Async variant of <see cref="SwitchToFrame(OpenQA.Selenium.By)" />.</summary>
    public Task SwitchToFrameAsync(By by, CancellationToken ct = default)
        => Task.Run(
            () => {
                ct.ThrowIfCancellationRequested();
                SwitchToFrame(by);
            },
            ct);

    /// <summary>Enters a frame and returns a scope that calls <see cref="SwitchToParentFrame" /> on dispose.</summary>
    public FrameScope EnterFrame(By by)
    {
        ArgumentHelpers.ThrowIfNull(by, nameof(by));
        var el = _scraper.WaitFor(by);
        OperationHelpers.ThrowIfNull(el, $"Frame not found: {by}");
        return EnterFrame(el);
    }

    /// <summary>Enters a frame and returns a scope that calls <see cref="SwitchToParentFrame" /> on dispose.</summary>
    public FrameScope EnterFrame(IWebElement frame)
    {
        SwitchToFrame(frame);
        return new FrameScope(this);
    }

    /// <summary>Async <see cref="EnterFrame(OpenQA.Selenium.By)" />.</summary>
    public async Task<FrameScope> EnterFrameAsync(By by, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(by, nameof(by));
        var el = await _scraper.WaitForAsync(by, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(el, $"Frame not found: {by}");
        return EnterFrame(el);
    }

    private void RunFrameOp(string operation, Action action)
    {
        var log = _scraper.Logger;
        log.LogDebug("Frame operation {Operation} starting", operation);
        try {
            using (_scraper.Metrics.StartTimer(_scraper.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), SeleniumMetricTags.ForOperation(_scraper, operation))) {
                action();
            }

            _scraper.Metrics.IncrementCounter(
                _scraper.ResolveMetric(nameof(Wm.Metrics.FrameOperation)),
                tags: SeleniumMetricTags.ForOperation(_scraper, operation, new[] { ("result", "success") }));
            log.LogDebug("Frame operation {Operation} completed", operation);
        }
        catch (Exception ex) {
            _scraper.Metrics.IncrementCounter(
                _scraper.ResolveMetric(nameof(Wm.Metrics.FrameOperation)),
                tags: SeleniumMetricTags.ForOperation(_scraper, operation, new[] { ("result", "failure") }));
            _scraper.Metrics.RecordError(_scraper.ResolveMetric(nameof(Wm.Metrics.FrameOperationDuration)), ex, SeleniumMetricTags.ForOperation(_scraper, operation));
            log.LogWarning(ex, "Frame operation {Operation} failed", operation);
            throw;
        }
    }
}

/// <summary>Restores the parent frame when disposed (one <see cref="FrameNavigator.SwitchToParentFrame" />).</summary>
public sealed class FrameScope : IDisposable
{
    private readonly FrameNavigator _navigator;
    private int _disposed;

    internal FrameScope(FrameNavigator navigator)
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
