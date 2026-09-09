using Lyo.Web.Primitives.DataGrid;
using MudBlazor;
using MudBlazor.Services;

namespace Lyo.Web.Components.Tests;

public class LyoViewportWatcherTests
{
    [Fact]
    public void Current_BeforeTheFirstReport_IsNone() => Assert.Equal(Breakpoint.None, new LyoViewportWatcher().Current);

    [Fact]
    public void IsAtOrBelow_BeforeTheFirstReport_IsFalse()
    {
        var watcher = new LyoViewportWatcher();

        Assert.False(watcher.IsAtOrBelow(Breakpoint.Sm));
        Assert.False(watcher.IsAtOrBelow(Breakpoint.Xl));
    }

    [Theory]
    [InlineData(Breakpoint.Xs, Breakpoint.Sm, true)]
    [InlineData(Breakpoint.Sm, Breakpoint.Sm, true)]
    [InlineData(Breakpoint.Md, Breakpoint.Sm, false)]
    [InlineData(Breakpoint.Lg, Breakpoint.Sm, false)]
    [InlineData(Breakpoint.Md, Breakpoint.Md, true)]
    [InlineData(Breakpoint.Xxl, Breakpoint.Xl, false)]
    [InlineData(Breakpoint.Xl, Breakpoint.Xxl, true)]
    public async Task IsAtOrBelow_ComparesTheReportedBreakpointWithTheReference(Breakpoint reported, Breakpoint reference, bool expected)
    {
        var watcher = await WatchingAsync(reported);

        Assert.Equal(expected, watcher.IsAtOrBelow(reference));
    }

    [Theory]
    [InlineData(Breakpoint.SmAndDown, Breakpoint.Sm, true)]
    [InlineData(Breakpoint.MdAndDown, Breakpoint.Sm, false)]
    [InlineData(Breakpoint.MdAndDown, Breakpoint.Md, true)]
    public async Task IsAtOrBelow_TreatsAndDownBreakpointsAsTheirUpperBound(Breakpoint reported, Breakpoint reference, bool expected)
    {
        var watcher = await WatchingAsync(reported);

        Assert.Equal(expected, watcher.IsAtOrBelow(reference));
    }

    [Fact]
    public async Task IsAtOrBelow_UnrankedReference_IsFalse()
    {
        var watcher = await WatchingAsync(Breakpoint.Xs);

        Assert.False(watcher.IsAtOrBelow(Breakpoint.Always));
        Assert.False(watcher.IsAtOrBelow(Breakpoint.None));
    }

    [Fact]
    public async Task StartAsync_NullService_LeavesTheWatcherInert()
    {
        var watcher = new LyoViewportWatcher();
        var notified = false;

        await watcher.StartAsync(
            null, () => {
                notified = true;
                return Task.CompletedTask;
            });

        Assert.False(notified);
        Assert.Equal(Breakpoint.None, watcher.Current);
    }

    [Fact]
    public async Task StartAsync_LaterReports_UpdateCurrentAndNotify()
    {
        var service = new FakeBrowserViewportService(Breakpoint.Lg);
        var watcher = new LyoViewportWatcher();
        var notifications = 0;

        await watcher.StartAsync(
            service, () => {
                notifications++;
                return Task.CompletedTask;
            });
        await service.ReportAsync(Breakpoint.Xs);

        Assert.Equal(Breakpoint.Xs, watcher.Current);
        Assert.True(watcher.IsAtOrBelow(Breakpoint.Sm));
        Assert.Equal(2, notifications);
    }

    [Fact]
    public async Task StartAsync_CalledTwice_KeepsTheFirstSubscription()
    {
        var first = new FakeBrowserViewportService(Breakpoint.Xs);
        var second = new FakeBrowserViewportService(Breakpoint.Xl);
        var watcher = new LyoViewportWatcher();

        await watcher.StartAsync(first, () => Task.CompletedTask);
        await watcher.StartAsync(second, () => Task.CompletedTask);

        Assert.Equal(Breakpoint.Xs, watcher.Current);
        Assert.Equal(0, second.SubscriberCount);
    }

    [Fact]
    public async Task StartAsync_ServiceThatThrows_DoesNotBubbleUp()
    {
        var watcher = new LyoViewportWatcher();

        await watcher.StartAsync(new ThrowingBrowserViewportService(), () => Task.CompletedTask);

        Assert.Equal(Breakpoint.None, watcher.Current);
    }

    [Fact]
    public async Task DisposeAsync_Unsubscribes()
    {
        var service = new FakeBrowserViewportService(Breakpoint.Md);
        var watcher = new LyoViewportWatcher();

        await watcher.StartAsync(service, () => Task.CompletedTask);
        await watcher.DisposeAsync();

        Assert.Equal(0, service.SubscriberCount);
    }

    private static async Task<LyoViewportWatcher> WatchingAsync(Breakpoint breakpoint)
    {
        var watcher = new LyoViewportWatcher();
        await watcher.StartAsync(new FakeBrowserViewportService(breakpoint), () => Task.CompletedTask);
        return watcher;
    }

    /// <summary>Tiny stand-in for MudBlazor's viewport service: records observers and replays breakpoints on demand.</summary>
    private sealed class FakeBrowserViewportService(Breakpoint initial) : IBrowserViewportService
    {
        private readonly Dictionary<Guid, Func<BrowserViewportEventArgs, Task>> _observers = [];

        public int SubscriberCount => _observers.Count;

        public ResizeOptions ResizeOptions { get; } = new();

        public async Task ReportAsync(Breakpoint breakpoint)
        {
            foreach (var observer in _observers.Values.ToList())
                await observer(new BrowserViewportEventArgs(Guid.NewGuid(), new BrowserWindowSize(), breakpoint, false));
        }

        public Task SubscribeAsync(Guid observerId, Func<BrowserViewportEventArgs, Task> lambda, ResizeOptions? options = null, bool fireImmediately = true)
        {
            _observers[observerId] = lambda;
            return fireImmediately ? lambda(new BrowserViewportEventArgs(observerId, new BrowserWindowSize(), initial, true)) : Task.CompletedTask;
        }

        public Task SubscribeAsync(Guid observerId, Action<BrowserViewportEventArgs> lambda, ResizeOptions? options = null, bool fireImmediately = true)
            => SubscribeAsync(
                observerId, args => {
                    lambda(args);
                    return Task.CompletedTask;
                }, options, fireImmediately);

        public Task SubscribeAsync(IBrowserViewportObserver observer, bool fireImmediately = true)
            => SubscribeAsync(observer.Id, observer.NotifyBrowserViewportChangeAsync, observer.ResizeOptions, fireImmediately);

        public Task UnsubscribeAsync(Guid observerId)
        {
            _observers.Remove(observerId);
            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(IBrowserViewportObserver observer) => UnsubscribeAsync(observer.Id);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<BrowserWindowSize> GetCurrentBrowserWindowSizeAsync() => Task.FromResult(new BrowserWindowSize());

        public Task<bool> IsBreakpointWithinWindowSizeAsync(Breakpoint breakpoint) => Task.FromResult(false);

        public Task<bool> IsBreakpointWithinReferenceSizeAsync(Breakpoint breakpoint, Breakpoint referenceBreakpoint) => Task.FromResult(false);

        public Task<bool> IsMediaQueryMatchAsync(string mediaQuery) => Task.FromResult(false);

        public Task<Breakpoint> GetCurrentBreakpointAsync() => Task.FromResult(initial);
    }

    /// <summary>Stand-in for a circuit where JS interop is unavailable, so every subscription attempt throws.</summary>
    private sealed class ThrowingBrowserViewportService : IBrowserViewportService
    {
        public ResizeOptions ResizeOptions { get; } = new();

        public Task SubscribeAsync(Guid observerId, Func<BrowserViewportEventArgs, Task> lambda, ResizeOptions? options = null, bool fireImmediately = true)
            => throw new InvalidOperationException("JavaScript interop is unavailable during prerender.");

        public Task SubscribeAsync(Guid observerId, Action<BrowserViewportEventArgs> lambda, ResizeOptions? options = null, bool fireImmediately = true)
            => throw new InvalidOperationException("JavaScript interop is unavailable during prerender.");

        public Task SubscribeAsync(IBrowserViewportObserver observer, bool fireImmediately = true)
            => throw new InvalidOperationException("JavaScript interop is unavailable during prerender.");

        public Task UnsubscribeAsync(Guid observerId) => Task.CompletedTask;

        public Task UnsubscribeAsync(IBrowserViewportObserver observer) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<BrowserWindowSize> GetCurrentBrowserWindowSizeAsync() => Task.FromResult(new BrowserWindowSize());

        public Task<bool> IsBreakpointWithinWindowSizeAsync(Breakpoint breakpoint) => Task.FromResult(false);

        public Task<bool> IsBreakpointWithinReferenceSizeAsync(Breakpoint breakpoint, Breakpoint referenceBreakpoint) => Task.FromResult(false);

        public Task<bool> IsMediaQueryMatchAsync(string mediaQuery) => Task.FromResult(false);

        public Task<Breakpoint> GetCurrentBreakpointAsync() => Task.FromResult(Breakpoint.None);
    }
}
