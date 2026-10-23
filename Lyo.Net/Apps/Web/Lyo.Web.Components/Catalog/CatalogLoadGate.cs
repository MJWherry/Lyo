namespace Lyo.Web.Components.Catalog;

/// <summary>Tracks one in-flight catalog fetch. A repeat of the same package and base path does not start another request, and a response from an older request is ignored.</summary>
internal sealed class CatalogLoadGate : IDisposable
{
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private string? _key;
    private int _requestId;

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>Key for <paramref name="basePath" /> plus <paramref name="packageId" />.</summary>
    public static string Key(string? basePath, string? packageId) => $"{basePath}\n{packageId}";

    /// <summary>True when <paramref name="key" /> is the request currently in flight or already completed.</summary>
    public bool IsCurrentRequest(string key) => !_disposed && string.Equals(_key, key, StringComparison.Ordinal);

    /// <summary>Starts a request for <paramref name="key" /> and cancels the previous one.</summary>
    public (int RequestId, CancellationToken Token) Begin(string key)
    {
        _key = key;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new();
        return (++_requestId, _cts.Token);
    }

    /// <summary>True when <paramref name="requestId" /> is still the latest <see cref="Begin" />.</summary>
    public bool IsCurrent(int requestId) => !_disposed && requestId == _requestId;
}