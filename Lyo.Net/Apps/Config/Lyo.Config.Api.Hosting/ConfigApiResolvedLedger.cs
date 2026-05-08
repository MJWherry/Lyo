using Lyo.Exceptions;
using Microsoft.Extensions.Primitives;

namespace Lyo.Config.Api.Hosting;

/// <summary>Thread-safe latest <see cref="ResolvedConfigRecord" /> plus reload tokens for options monitors.</summary>
public sealed class ConfigApiResolvedLedger
{
    private readonly object _dataLock = new();
    private readonly object _tokenLock = new();
    private CancellationTokenSource _cts = new();
    private string? _etag;

    private ResolvedConfigRecord? _record;

    /// <summary>Latest resolved payload (null until first successful resolve).</summary>
    public ResolvedConfigRecord? Current {
        get {
            lock (_dataLock)
                return _record;
        }
    }

    /// <summary>Opaque ETag from the last successful HTTP response, for <c>If-None-Match</c> probes.</summary>
    public string? CurrentEtag {
        get {
            lock (_dataLock)
                return _etag;
        }
    }

    /// <summary>Change token invalidated when <see cref="SetResolved" /> runs after a new snapshot.</summary>
    public IChangeToken GetReloadToken()
    {
        lock (_tokenLock)
            return new CancellationChangeToken(_cts.Token);
    }

    /// <summary>Atomically updates the snapshot and notifies change listeners.</summary>
    public void SetResolved(ResolvedConfigRecord resolved, string? etag)
    {
        ArgumentHelpers.ThrowIfNull(resolved);
        lock (_dataLock) {
            _record = resolved;
            _etag = etag;
        }

        CancellationTokenSource oldCts;
        lock (_tokenLock) {
            oldCts = _cts;
            _cts = new();
        }

        try {
            oldCts.Cancel();
        }
        finally {
            oldCts.Dispose();
        }
    }
}