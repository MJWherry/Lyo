namespace Lyo.Config.Api.Models;

/// <summary>GET/HEAD resolve result: optional body plus the opaque ETag to send as If-None-Match on the next poll.</summary>
public sealed class ConfigResolveConditionalResult(ConfigResolveOutcome outcome, string? etag, ResolvedConfigRecord? resolved, HttpStatusDescriptor? failure = null)
{
    public ConfigResolveOutcome Outcome { get; } = outcome;

    /// <summary>Quoted HTTP entity tag; pass it on the next conditional resolve.</summary>
    public string? ETag { get; } = etag;

    public ResolvedConfigRecord? Resolved { get; } = resolved;

    public HttpStatusDescriptor? Failure { get; } = failure;
}

/// <summary>Minimal status payload for <see cref="ConfigResolveOutcome.Failed" /> (non-success HTTP).</summary>
public sealed class HttpStatusDescriptor(int statusCode, string reasonPhrase)
{
    public int StatusCode { get; } = statusCode;

    public string ReasonPhrase { get; } = reasonPhrase;
}