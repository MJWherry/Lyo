using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>Helpers for creating IDisposable and IAsyncDisposable from actions.</summary>
public static class Disposable
{
    /// <summary>Creates an IDisposable that invokes the action when disposed.</summary>
    public static IDisposable Create(Action onDispose)
    {
        ArgumentHelpers.ThrowIfNull(onDispose);
        return new ActionDisposable(onDispose);
    }

    /// <summary>Creates an IAsyncDisposable that invokes the async action when disposed.</summary>
    public static IAsyncDisposable CreateAsync(Func<ValueTask> onDispose)
    {
        ArgumentHelpers.ThrowIfNull(onDispose);
        return new AsyncActionDisposable(onDispose);
    }

    private sealed class ActionDisposable(Action onDispose) : IDisposable
    {
        private readonly object _lock = new();
        private Action? _onDispose = onDispose;

        public void Dispose()
        {
            Action? toInvoke;
            lock (_lock) {
                toInvoke = _onDispose;
                _onDispose = null;
            }

            toInvoke?.Invoke();
        }
    }

    private sealed class AsyncActionDisposable(Func<ValueTask> onDispose) : IAsyncDisposable
    {
        private readonly object _lock = new();
        private Func<ValueTask>? _onDispose = onDispose;

        public async ValueTask DisposeAsync()
        {
            Func<ValueTask>? toInvoke;
            lock (_lock) {
                toInvoke = _onDispose;
                _onDispose = null;
            }

            if (toInvoke != null)
                await toInvoke().ConfigureAwait(false);
        }
    }
}