namespace Lyo.Http.Client;

/// <summary>Compose-without-subclassing callbacks for a single <see cref="LyoHttpClient" />. Observer callbacks see only this client's traffic.</summary>
public sealed class LyoHttpClientHooks
{
    /// <summary>Runs before send; may mutate the request.</summary>
    public Func<LyoHttpCallContext, CancellationToken, Task>? BeforeSendAsync { get; set; }

    /// <summary>Runs after a response is received (success or failure status).</summary>
    public Func<LyoHttpCallContext, CancellationToken, Task>? AfterResponseAsync { get; set; }

    /// <summary>Runs when the call throws or a non-success status is mapped to an exception.</summary>
    public Func<LyoHttpCallContext, CancellationToken, Task>? OnFailureAsync { get; set; }

    /// <summary>Reserved for host retry pipelines (Polly). Not invoked by the client itself.</summary>
    public Func<LyoHttpCallContext, CancellationToken, Task>? OnRetryAsync { get; set; }

    /// <summary>Records an outbound request this client is about to send (host/path/content-type filters belong here).</summary>
    public Func<LyoHttpCallContext, CancellationToken, Task>? OnRequestObserved { get; set; }

    /// <summary>Records the response this client received.</summary>
    public Func<LyoHttpCallContext, CancellationToken, Task>? OnResponseObserved { get; set; }
}
