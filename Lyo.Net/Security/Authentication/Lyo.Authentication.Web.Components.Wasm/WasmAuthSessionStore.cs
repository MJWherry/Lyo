using Blazored.LocalStorage;
using Lyo.Exceptions;
namespace Lyo.Authentication.Web.Components.Wasm;

/// <summary>
/// In-memory plus LocalStorage-backed session store for the WASM auth runtime. The current snapshot is cached in memory so the delegating handler can inject the bearer
/// synchronously; writes are mirrored to <c>Blazored.LocalStorage</c> so the session survives a page refresh inside the SPA.
/// </summary>
public sealed class WasmAuthSessionStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILocalStorageService _localStorage;
    private readonly WasmAuthClientOptions _options;
    private WasmAuthPersistedSession? _current;
    private bool _loaded;

    /// <summary>Builds a new store.</summary>
    public WasmAuthSessionStore(ILocalStorageService localStorage, WasmAuthClientOptions options)
    {
        ArgumentHelpers.ThrowIfNull(localStorage);
        ArgumentHelpers.ThrowIfNull(options);
        _localStorage = localStorage;
        _options = options;
    }

    /// <summary>Raised whenever the active session changes (sign-in, refresh, or sign-out). The argument is <c>null</c> on sign-out.</summary>
    public event Action<WasmAuthPersistedSession?>? Changed;

    /// <summary>Cached snapshot, hydrating from LocalStorage on first call. <c>null</c> when nothing is stored or hydration is impossible (e.g. pre-render).</summary>
    public async Task<WasmAuthPersistedSession?> GetAsync(CancellationToken ct = default)
    {
        if (_loaded)
            return _current;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_loaded)
                return _current;

            try {
                _current = await _localStorage.GetItemAsync<WasmAuthPersistedSession?>(_options.StorageKey, ct).ConfigureAwait(false);
            }
            catch (Exception) {
                _current = null;
            }

            _loaded = true;
            return _current;
        }
        finally {
            _gate.Release();
        }
    }

    /// <summary>Returns the cached snapshot without touching LocalStorage. Useful in synchronous paths (e.g. <c>WasmAuthDelegatingHandler.SendAsync</c>'s fast path).</summary>
    public WasmAuthPersistedSession? Peek() => _current;

    /// <summary>Stores a new snapshot in memory and LocalStorage and raises <see cref="Changed" />.</summary>
    public async Task SetAsync(WasmAuthPersistedSession snapshot, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(snapshot);
        _current = snapshot;
        _loaded = true;
        try {
            await _localStorage.SetItemAsync(_options.StorageKey, snapshot, ct).ConfigureAwait(false);
        }
        catch (Exception) {
            // best-effort: prerendering or JS unavailable
        }

        Changed?.Invoke(snapshot);
    }

    /// <summary>Clears the active snapshot from memory and LocalStorage and raises <see cref="Changed" /> with <c>null</c>.</summary>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        _current = null;
        _loaded = true;
        try {
            await _localStorage.RemoveItemAsync(_options.StorageKey, ct).ConfigureAwait(false);
        }
        catch (Exception) {
            // best-effort
        }

        Changed?.Invoke(null);
    }
}