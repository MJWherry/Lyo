using System.Diagnostics.CodeAnalysis;
using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>Helpers for creating IDisposable and IAsyncDisposable from actions.</summary>
public static class Disposable
{
    /// <summary>Creates an IDisposable that invokes the action when disposed.</summary>
    [return: NotNull]
    public static IDisposable Create([NotNull] Action onDispose)
    {
        ArgumentHelpers.ThrowIfNull(onDispose, nameof(onDispose));
        return new ActionDisposable(onDispose);
    }

    /// <summary>Creates an IAsyncDisposable that invokes the async action when disposed.</summary>
    [return: NotNull]
    public static IAsyncDisposable CreateAsync([NotNull] Func<ValueTask> onDispose)
    {
        ArgumentHelpers.ThrowIfNull(onDispose, nameof(onDispose));
        return new AsyncActionDisposable(onDispose);
    }

    private sealed class ActionDisposable : IDisposable
    {
        private readonly object _lock = new();
        private Action? _onDispose;

        public ActionDisposable(Action onDispose) => _onDispose = onDispose;

        public void Dispose()
        {
            Action? toInvoke = null;
            lock (_lock) {
                toInvoke = _onDispose;
                _onDispose = null;
            }

            toInvoke?.Invoke();
        }
    }

    private sealed class AsyncActionDisposable : IAsyncDisposable
    {
        private readonly object _lock = new();
        private Func<ValueTask>? _onDispose;

        public AsyncActionDisposable(Func<ValueTask> onDispose) => _onDispose = onDispose;

        public async ValueTask DisposeAsync()
        {
            Func<ValueTask>? toInvoke = null;
            lock (_lock) {
                toInvoke = _onDispose;
                _onDispose = null;
            }

            if (toInvoke != null)
                await toInvoke().ConfigureAwait(false);
        }
    }
}