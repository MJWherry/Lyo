namespace Lyo.Webhook;

/// <summary>Raw request data used to verify a webhook signature. Body bytes must match the payload the sender signed.</summary>
public sealed class WebhookVerificationContext
{
    /// <summary>Raw request body — the exact bytes the provider signed.</summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>HTTP headers (compare well-known names ignore-case).</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Full public URL (scheme, host, path, query) when the provider folds it into the signed string.</summary>
    public string? RequestUrl { get; set; }

    /// <summary>Form or query parameters when the provider signs them (for example application/x-www-form-urlencoded).</summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; set; }

    /// <summary>Arbitrary provider-specific state (clock-skew tolerance, key id, and similar).</summary>
    public IWebhookVerificationOptions? Options { get; set; }

    public WebhookVerificationContext(ReadOnlyMemory<byte> body, IReadOnlyDictionary<string, string> headers)
    {
        Body = body;
        Headers = headers;
    }
}

/// <summary>Optional settings bag for provider verifiers. Implement this in provider libraries.</summary>
public interface IWebhookVerificationOptions { }