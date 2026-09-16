using Lyo.Exceptions;
using PuppeteerSharp;

namespace Lyo.Web.Automation.PuppeteerSharp.Browser;

/// <summary>JavaScript dialog helpers via <see cref="IPage.Dialog" />.</summary>
public sealed class PuppeteerSharpDialogs
{
    private readonly PuppeteerSharpBrowser _browser;

    internal PuppeteerSharpDialogs(PuppeteerSharpBrowser browser)
    {
        ArgumentHelpers.ThrowIfNull(browser);
        _browser = browser;
    }

    /// <summary>Adds a handler that auto-accepts every dialog until disposed.</summary>
    public IDisposable AutoAcceptAll()
    {
        var page = _browser.GetRequiredPage();
        page.Dialog += Handler;
        return new ActionDisposable(() => page.Dialog -= Handler);

        void Handler(object? _, DialogEventArgs e) => e.Dialog.Accept();
    }

    /// <summary>Accepts the next dialog, then unsubscribes.</summary>
    public void AcceptNext()
    {
        var page = _browser.GetRequiredPage();
        page.Dialog += Handler;
        return;

        void Handler(object? _, DialogEventArgs e)
        {
            page.Dialog -= Handler;
            e.Dialog.Accept();
        }
    }

    /// <summary>Waits for the next dialog and returns its message.</summary>
    public async Task<string> WaitForMessageAsync(CancellationToken ct = default)
    {
        var page = _browser.GetRequiredPage();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? _, DialogEventArgs e)
        {
            page.Dialog -= Handler;
            tcs.TrySetResult(e.Dialog.Message);
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
