namespace Lyo.Http.Client;

/// <summary>Request markers for handlers (download streaming vs ThroughSolver HTML).</summary>
public static class LyoHttpRequestMarkers
{
    /// <summary>When true, Flared must not send the body through FlareSolverr's JSON envelope.</summary>
    public const string StreamBinary = "Lyo.Http.StreamBinary";

    /// <summary>Marks <paramref name="request" /> as a binary/stream download.</summary>
    public static void SetStreamBinary(HttpRequestMessage request)
    {
#if NET5_0_OR_GREATER
        request.Options.Set(new HttpRequestOptionsKey<bool>(StreamBinary), true);
#else
        request.Properties[StreamBinary] = true;
#endif
    }

    /// <summary>True when this request should skip ThroughSolver.</summary>
    public static bool IsStreamBinary(HttpRequestMessage request)
    {
#if NET5_0_OR_GREATER
        return request.Options.TryGetValue(new HttpRequestOptionsKey<bool>(StreamBinary), out var value) && value;
#else
        return request.Properties.TryGetValue(StreamBinary, out var value) && value is true;
#endif
    }
}
