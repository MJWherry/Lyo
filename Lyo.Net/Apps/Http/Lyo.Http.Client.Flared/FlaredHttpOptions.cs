using Lyo.Exceptions;
using Lyo.Http.Client;

namespace Lyo.Http.Client.Flared;

/// <summary>
/// Options for <see cref="FlaredHttpMessageHandler" />. Timing, rate limit, and PerSession UA are on by default.
/// <see cref="LyoHttpClientOptions.ProxyUrl" /> is sent to FlareSolverr and to the primary handler.
/// </summary>
public class FlaredHttpOptions : LyoHttpClientOptions
{
    /// <summary>Configuration section name.</summary>
    public new const string SectionName = "FlaredHttp";

    /// <summary>Named <c>IHttpClientFactory</c> client. Isolated from vendor buckets.</summary>
    public const string HttpClientName = "Lyo.Http.Flared";

    /// <summary>FlareSolverr base URL (no /v1 suffix required). Default localhost:8191.</summary>
    public string FlareSolverrUrl { get; set; } = "http://127.0.0.1:8191/";

    /// <summary>Max time for FlareSolverr to solve, in milliseconds. Must be at least 15000.</summary>
    public int MaxTimeoutMs { get; set; } = 60000;

    /// <summary>Create a FlareSolverr session on first call and destroy it on dispose.</summary>
    public bool UseSolverSessions { get; set; } = true;

    /// <summary>ThroughSolver (default) or ClearanceHandler replay.</summary>
    public FlaredFetchMode FetchMode { get; set; } = FlaredFetchMode.ThroughSolver;

    /// <summary>Accept-Language sent on solver requests when set.</summary>
    public string? AcceptLanguage { get; set; } = "en-US,en;q=0.9";

    /// <summary>Extra headers copied onto the solver request (not Sec-Fetch-* inventions).</summary>
    public Dictionary<string, string>? ExtraHeaders { get; set; }

    /// <summary>After a solve, invoked with cookies and UA (optional host hook).</summary>
    public Func<IFlareSession, CancellationToken, Task>? OnChallengeSolvedAsync { get; set; }

    /// <summary>Applies Flared defaults (timing, rate limit, PerSession UA).</summary>
    public FlaredHttpOptions()
    {
        Timing.Enabled = true;
        Timing.DelayBeforeSendMin = TimeSpan.FromMilliseconds(200);
        Timing.DelayBeforeSendMax = TimeSpan.FromMilliseconds(800);
        Timing.Jitter = 0.2;
        RateLimit.Enabled = true;
        RateLimit.PermitLimit = 1;
        RateLimit.Window = TimeSpan.FromSeconds(2);
        RateLimit.Jitter = 0.2;
        RateLimit.HonorRetryAfter = true;
        RateLimit.MaxConcurrent = 2;
        UserAgent.Enabled = true;
        UserAgent.Rotation = LyoHttpUserAgentRotation.PerSession;
    }

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(FlareSolverrUrl);
        ArgumentHelpers.ThrowIf(MaxTimeoutMs < 15000, "MaxTimeoutMs must be >= 15000.");
        if (UserAgent.Rotation == LyoHttpUserAgentRotation.PerRequest)
            throw new InvalidOperationException("FlaredHttpOptions cannot use PerRequest User-Agent rotation; clearance cookies are bound to one UA per session.");
    }
}
