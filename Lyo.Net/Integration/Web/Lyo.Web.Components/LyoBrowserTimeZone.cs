using Lyo.Exceptions;
using Microsoft.JSInterop;

namespace Lyo.Web.Components;

/// <summary>Circuit-scoped cache of <see cref="IJsInterop.GetClientTimeZoneInfo" />. JS failures are not cached so prerender can retry after the circuit connects.</summary>
public sealed class LyoBrowserTimeZone(IJsInterop js) : ILyoTimeZone
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TimeZoneInfo? _zone;

    /// <inheritdoc/>
    public async Task<TimeZoneInfo> GetAsync(CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(js);
        if (_zone != null)
            return _zone;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (_zone != null)
                return _zone;

            try {
                _zone = await js.GetClientTimeZoneInfo().ConfigureAwait(false);
                return _zone;
            }
            catch (JSException) {
                return TimeZoneInfo.Utc;
            }
            catch (JSDisconnectedException) {
                return TimeZoneInfo.Utc;
            }
            catch (InvalidOperationException) {
                return TimeZoneInfo.Utc;
            }
        }
        finally {
            _gate.Release();
        }
    }
}
