using Microsoft.AspNetCore.Http;

namespace Lyo.Webhook;

/// <summary>Passed to a webhook handler once the signature has been verified.</summary>
public sealed class WebhookHandlerContext
{
    /// <summary>Current <see cref="HttpContext" />.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>Raw request body (the same bytes used for verification).</summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>Token that fires when the request is aborted.</summary>
    public CancellationToken CancellationToken => HttpContext.RequestAborted;

    public WebhookHandlerContext(HttpContext httpContext, ReadOnlyMemory<byte> body)
    {
        HttpContext = httpContext;
        Body = body;
    }
}

/// <summary>Passed to a handler when the body has been verified and deserialized as <typeparamref name="TRequest" />.</summary>
public sealed class WebhookRequestContext<TRequest>
{
    /// <summary>Current <see cref="HttpContext" />.</summary>
    public HttpContext HttpContext { get; }

    /// <summary>Deserialized request payload.</summary>
    public TRequest Request { get; }

    /// <summary>Original body bytes as received.</summary>
    public ReadOnlyMemory<byte> RawBody { get; }

    /// <summary>Token that fires when the request is aborted.</summary>
    public CancellationToken CancellationToken => HttpContext.RequestAborted;

    public WebhookRequestContext(HttpContext httpContext, TRequest request, ReadOnlyMemory<byte> rawBody)
    {
        HttpContext = httpContext;
        Request = request;
        RawBody = rawBody;
    }
}