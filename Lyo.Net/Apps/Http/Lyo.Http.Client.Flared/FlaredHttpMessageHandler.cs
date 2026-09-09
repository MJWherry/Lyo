using System.Net;
using System.Text;
using FlareSolverrSharp.Solvers;
using FlareSolverrSharp.Types;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Http.Client;
using Lyo.Http.Client.Session;
using FlareCookie = FlareSolverrSharp.Types.Cookie;

namespace Lyo.Http.Client.Flared;

/// <summary>
/// FlareSolverr /v1 adapter. ThroughSolver maps the request to <c>request.get</c>/<c>request.post</c> and returns a synthetic
/// <see cref="HttpResponseMessage" /> from <c>solution.response</c>. Binary downloads skip the solver and use the inner handler with cookies+UA.
/// </summary>
public sealed class FlaredHttpMessageHandler : DelegatingHandler
{
    private readonly FlaredHttpOptions _options;
    private readonly FlareSolverr _solver;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private string? _solverSessionId;

    /// <summary>Creates the handler. Inner handler must be left null when added via <c>IHttpClientFactory</c>.</summary>
    public FlaredHttpMessageHandler(FlaredHttpOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _options = options;
        _solver = new FlareSolverr(options.FlareSolverrUrl) {
            MaxTimeout = options.MaxTimeoutMs,
            ProxyUrl = options.ProxyUrl ?? "",
            ProxyUsername = options.ProxyUsername,
            ProxyPassword = options.ProxyPassword
        };
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        if (LyoHttpRequestMarkers.IsStreamBinary(request) || _options.FetchMode == FlaredFetchMode.ReplayWithClearanceHandler)
            return await base.SendAsync(request, ct).ConfigureAwait(false);

        ApplySolverHeaders(request);
        var sessionId = await EnsureSolverSessionAsync(ct).ConfigureAwait(false);
        var solved = await _solver.Solve(request, sessionId ?? "").ConfigureAwait(false);
        ArgumentHelpers.ThrowIfNull(solved);
        ArgumentHelpers.ThrowIfNull(solved.Solution);
        MergeSolution(solved);
        if (_options.OnChallengeSolvedAsync != null && LyoHttpSessionAccessor.Session is IFlareSession flare)
            await _options.OnChallengeSolvedAsync(flare, ct).ConfigureAwait(false);

        return ToResponse(request, solved.Solution);
    }

    /// <summary>Maps FlareSolverr cookies and UA into the ambient session.</summary>
    internal void MergeSolution(FlareSolverrResponse solved)
    {
        var session = LyoHttpSessionAccessor.Session;
        if (solved.Solution.UserAgent != null) {
            if (session != null)
                session.UserAgent = solved.Solution.UserAgent;
        }

        if (solved.Solution.Cookies == null || session == null)
            return;

        foreach (var cookie in solved.Solution.Cookies) {
            session.CookieJar.Add(new LyoHttpCookie {
                Name = cookie.Name,
                Value = cookie.Value,
                Domain = cookie.Domain,
                Path = string.IsNullOrWhiteSpace(cookie.Path) ? "/" : cookie.Path,
                Secure = cookie.Secure,
                HttpOnly = cookie.HttpOnly,
                Expiry = ToExpiry(cookie)
            });
        }
    }

    internal static HttpResponseMessage ToResponse(HttpRequestMessage request, Solution solution)
    {
        var status = ToStatusCode(solution.Status);
        var response = new HttpResponseMessage(status) {
            RequestMessage = request,
            Content = new StringContent(solution.Response ?? "", Encoding.UTF8, "text/html")
        };
        response.Headers.TryAddWithoutValidation("X-FlareSolverr-Url", solution.Url);
        if (!string.IsNullOrWhiteSpace(solution.UserAgent))
            response.Headers.TryAddWithoutValidation("X-FlareSolverr-UserAgent", solution.UserAgent);

        return response;
    }

    private void ApplySolverHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.AcceptLanguage) && !request.Headers.Contains(HttpHeaderInfo.AcceptLanguage))
            request.Headers.TryAddWithoutValidation(HttpHeaderInfo.AcceptLanguage, _options.AcceptLanguage);

        if (_options.ExtraHeaders == null)
            return;

        foreach (var pair in _options.ExtraHeaders) {
            if (string.IsNullOrWhiteSpace(pair.Key) || request.Headers.Contains(pair.Key))
                continue;

            request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
        }
    }

    private static DateTimeOffset? ToExpiry(FlareCookie cookie)
    {
        if (cookie.Expires <= 0)
            return null;

        try {
            return DateTimeOffset.FromUnixTimeSeconds((long)cookie.Expires);
        }
        catch (ArgumentOutOfRangeException) {
            return null;
        }
    }

    private static HttpStatusCode ToStatusCode(string? status)
        => int.TryParse(status, out var code) && code is >= 100 and <= 599 ? (HttpStatusCode)code : HttpStatusCode.OK;

    private async Task<string?> EnsureSolverSessionAsync(CancellationToken ct)
    {
        if (!_options.UseSolverSessions)
            return null;

        if (_solverSessionId != null)
            return _solverSessionId;

        await _sessionGate.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_solverSessionId != null)
                return _solverSessionId;

            var created = await _solver.CreateSession().ConfigureAwait(false);
            _solverSessionId = created.Session;
            if (LyoHttpSessionAccessor.Session is IFlareSession flare)
                flare.FlareSolverrSessionId = _solverSessionId;

            return _solverSessionId;
        }
        finally {
            _sessionGate.Release();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) {
            if (_solverSessionId != null && _options.UseSolverSessions) {
                try {
                    _solver.DestroySession(_solverSessionId).GetAwaiter().GetResult();
                }
                catch {
                    // Best-effort session teardown
                }
            }

            _sessionGate.Dispose();
        }

        base.Dispose(disposing);
    }
}
