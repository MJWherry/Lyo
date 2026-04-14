using Lyo.Exceptions;

namespace Lyo.Pdf.Models;

/// <summary>Represents a scoped handle to a loaded PDF and unloads it when disposed.</summary>
public sealed class LoadedPdfLease : IDisposable, IAsyncDisposable
{
    private readonly Action<Guid> _unload;
    private readonly Func<Guid, Task> _unloadAsync;
    private int _isDisposed;

    public Guid Id { get; }

    public LoadedPdfLease(Guid id, Action<Guid> unload, Func<Guid, Task> unloadAsync)
    {
        ArgumentHelpers.ThrowIfNull(unload, nameof(unload));
        ArgumentHelpers.ThrowIfNull(unloadAsync, nameof(unloadAsync));
        Id = id;
        _unload = unload;
        _unloadAsync = unloadAsync;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        await _unloadAsync(Id).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        _unload(Id);
    }

    public static implicit operator Guid(LoadedPdfLease lease) => lease.Id;
}