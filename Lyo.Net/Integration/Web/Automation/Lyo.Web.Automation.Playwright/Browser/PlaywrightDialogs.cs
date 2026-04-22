using Lyo.Exceptions;
using Microsoft.Playwright;

namespace Lyo.Web.Automation.Playwright.Browser;

/// <summary>JavaScript dialog helpers (<c>alert</c>, <c>confirm</c>, <c>prompt</c>) via Playwright <see cref="IPage.Dialog" />.</summary>
public sealed class PlaywrightDialogs
{
    private readonly PlaywrightBrowser _browser;

    internal PlaywrightDialogs(PlaywrightBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser, nameof(browser));
        _browser = browser;
    }

    /// <summary>Registers a handler that auto-accepts every dialog until disposed.</summary>
    public IDisposable AutoAcceptAll()
    {
        var page = _browser.GetRequiredPage();

        void Handler(object? _, IDialog d) => _ = d.AcceptAsync();

        page.Dialog += Handler;
        return new ActionDisposable(() => page.Dialog -= Handler);
    }

    /// <summary>Registers a one-shot handler that accepts the next dialog, then unsubscribes.</summary>
    public void AcceptNext()
    {
        var page = _browser.GetRequiredPage();

        void Handler(object? _, IDialog d)
        {
            page.Dialog -= Handler;
            _ = d.AcceptAsync();
        }

        page.Dialog += Handler;
    }

    /// <summary>Waits for the next dialog and returns its message (blocks until a dialog is shown).</summary>
    public async Task<string> WaitForMessageAsync(CancellationToken ct = default)
    {
        var page = _browser.GetRequiredPage();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? _, IDialog d)
        {
            page.Dialog -= Handler;
            tcs.TrySetResult(d.Message);
        }

        page.Dialog += Handler;
        try {
            using (ct.Register(() => tcs.TrySetCanceled()))
                return await tcs.Task.ConfigureAwait(false);
        }
        finally {
            page.Dialog -= Handler;
        }
    }

    private sealed class ActionDisposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}