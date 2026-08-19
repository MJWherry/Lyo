namespace Lyo.Web.Components;

/// <summary>Browser IANA time zone for the current Blazor circuit, used when converting UTC timestamps for display.</summary>
public interface ILyoTimeZone
{
    /// <summary>Resolves the browser time zone (cached after the first successful JS read). Falls back to UTC when JS is unavailable without caching that failure.</summary>
    Task<TimeZoneInfo> GetAsync(CancellationToken cancellationToken = default);
}
