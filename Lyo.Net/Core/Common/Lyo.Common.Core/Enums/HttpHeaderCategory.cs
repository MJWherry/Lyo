namespace Lyo.Common.Core.Enums;

/// <summary>How well-known HTTP header catalog rows are grouped by purpose.</summary>
public enum HttpHeaderCategory
{
    /// <summary>Header is not known or not registered.</summary>
    Unknown = 0,

    /// <summary>Content negotiation (<c>Accept</c>, <c>Accept-Language</c>, <c>Accept-Encoding</c>).</summary>
    Negotiation,

    /// <summary>Representation metadata (<c>Content-Type</c>, <c>Content-Encoding</c>).</summary>
    Representation,

    /// <summary>Cookie exchange (<c>Cookie</c>, <c>Set-Cookie</c>).</summary>
    Cookie,

    /// <summary>Client identity and origin (<c>User-Agent</c>, <c>Origin</c>, <c>Referer</c>).</summary>
    Identity,

    /// <summary>Routing and retry hints (<c>Retry-After</c>, <c>Location</c>).</summary>
    Routing,

    /// <summary>Credentials and CSRF (<c>Authorization</c>, <c>X-Api-Key</c>, <c>X-CSRF-Token</c>).</summary>
    Auth,

    /// <summary>Request correlation and tracing (<c>X-Correlation-Id</c>, <c>X-Request-Id</c>, <c>X-Trace-Id</c>).</summary>
    Correlation,

    /// <summary>Reverse-proxy forwarding (<c>X-Forwarded-For</c>, <c>X-Real-IP</c>).</summary>
    Proxy,

    /// <summary>Quota windows advertised by the origin (<c>X-RateLimit-Limit</c>).</summary>
    RateLimit,

    /// <summary>Browser security policy (<c>X-Content-Type-Options</c>, <c>X-Frame-Options</c>).</summary>
    Security,

    /// <summary>Multi-tenant routing (<c>X-Tenant-Id</c>).</summary>
    Tenant,

    /// <summary>Replay protection (<c>Idempotency-Key</c>).</summary>
    Idempotency
}
