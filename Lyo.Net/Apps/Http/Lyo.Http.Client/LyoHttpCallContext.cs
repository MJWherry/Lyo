using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Http.Client;

/// <summary>Per-call context passed to <see cref="LyoHttpClientHooks" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LyoHttpCallContext
{
    /// <summary>Creates a context for <paramref name="request" />.</summary>
    public LyoHttpCallContext(HttpRequestMessage request)
    {
        ArgumentHelpers.ThrowIfNull(request);
        Request = request;
    }

    /// <summary>Outbound request (hooks may mutate it in <see cref="LyoHttpClientHooks.BeforeSendAsync" />).</summary>
    public HttpRequestMessage Request { get; }

    /// <summary>Response when one was received.</summary>
    public HttpResponseMessage? Response { get; set; }

    /// <summary>Failure, when the call threw.</summary>
    public Exception? Exception { get; set; }

    /// <summary>Elapsed time for this attempt.</summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>1-based attempt number (retries increment this).</summary>
    public int Attempt { get; set; } = 1;

    /// <summary>Per-call bag.</summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <inheritdoc />
    public override string ToString() => $"{Request.Method} {Request.RequestUri} attempt={Attempt}";
}
