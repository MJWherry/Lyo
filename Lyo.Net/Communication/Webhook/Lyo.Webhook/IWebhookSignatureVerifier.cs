namespace Lyo.Webhook;

/// <summary>Checks that an inbound HTTP request matches a provider's signing scheme (implemented in provider-specific libraries).</summary>
public interface IWebhookSignatureVerifier
{
    /// <summary>Validates the request with the configured secret or key material.</summary>
    WebhookVerificationResult Verify(WebhookVerificationContext context);
}