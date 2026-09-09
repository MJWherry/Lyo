using Lyo.Exceptions;
using Microsoft.Extensions.Primitives;

namespace Lyo.Config.Api.Hosting;

/// <summary>Thread-safe latest <see cref="ResolvedConfigRecord" /> and reload tokens for options monitors.</summary>
public sealed class ConfigApiResolvedLedger
{
    private readonly object _dataLock = new();
    private readonly object _tokenLock = new();
    private CancellationTokenSource _cts = new();
    private string? _etag;

    private ResolvedConfigRecord? _record;

    /// <summary>Most recent resolved payload. Null until the first successful resolve.</summary>
    public ResolvedConfigRecord? Current {
        get {
            lock (_dataLock)
                return _record;
        }
    }

    /// <summary>Opaque ETag from the last successful HTTP response, used on <c>If-None-Match</c> probes.</summary>
    public string? CurrentEtag {
        get {
            lock (_dataLock)
                return _etag;
        }
    }

    /// <summary>Change token cancelled after <see cref="SetResolved" /> stores a new snapshot.</summary>
    public IChangeToken GetReloadToken()
    {
        lock (_tokenLock)
            return new CancellationChangeToken(_cts.Token);
    }

    /// <summary>Replaces the snapshot atomically and wakes change listeners.</summary>
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