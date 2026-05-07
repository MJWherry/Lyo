namespace Lyo.Config.Api.Models;

/// <summary>Outcome of GET/HEAD resolved config including optional body and opaque ETag for If-None-Match on the next poll.</summary>
public sealed class ConfigResolveConditionalResult(ConfigResolveOutcome outcome, string? etag, ResolvedConfigRecord? resolved, HttpStatusDescriptor? failure = null)
{
    public ConfigResolveOutcome Outcome { get; } = outcome;

    /// <summary>Entity tag quoted per HTTP rules; reuse with conditional resolve on the next poll.</summary>
    public string? ETag { get; } = etag;

    public ResolvedConfigRecord? Resolved { get; } = resolved;

    public HttpStatusDescriptor? Failure { get; } = failure;
}

/// <summary>Lightweight descriptor when <see cref="ConfigResolveOutcome.Failed" /> (non-success).</summary>
public sealed class HttpStatusDescriptor(int statusCode, string reasonPhrase)
{
    public int StatusCode { get; } = statusCode;

    public string ReasonPhrase { get; } = reasonPhrase;
}