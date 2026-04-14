using Microsoft.AspNetCore.Http;

namespace Lyo.Webhook;

/// <summary>Passed to a webhook handler after the signature has been verified.</summary>
public sealed class WebhookHandlerContext
{
    /// <summary>Current HTTP context.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>Raw request body (same bytes used for verification).</summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>Request abort token.</summary>
    public CancellationToken CancellationToken => HttpContext.RequestAborted;

    public WebhookHandlerContext(HttpContext httpContext, ReadOnlyMemory<byte> body)
    {
        HttpContext = httpContext;
        Body = body;
    }
}

/// <summary>Passed to a handler when the body has been verified and deserialized to <typeparamref name="TRequest" />.</summary>
public sealed class WebhookRequestContext<TRequest>
{
    /// <summary>Current HTTP context.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>Deserialized payload.</summary>
    public TRequest Request { get; }

    /// <summary>Original body bytes.</summary>
    public ReadOnlyMemory<byte> RawBody { get; }

    /// <summary>Request abort token.</summary>
    public CancellationToken CancellationToken => HttpContext.RequestAborted;

    public WebhookRequestContext(HttpContext httpContext, TRequest request, ReadOnlyMemory<byte> rawBody)
    {
        HttpContext = httpContext;
        Request = request;
        RawBody = rawBody;
    }
}