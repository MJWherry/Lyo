namespace Lyo.Webhook;

/// <summary>Verifies that an inbound HTTP request matches a provider&apos;s signing scheme (implemented in provider-specific libraries).</summary>
public interface IWebhookSignatureVerifier
{
    /// <summary>Validates the request using the configured secret or key material.</summary>
    WebhookVerificationResult Verify(WebhookVerificationContext context);
}