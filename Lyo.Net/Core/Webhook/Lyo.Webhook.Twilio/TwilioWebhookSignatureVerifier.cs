using System.Security.Cryptography;
using System.Text;

namespace Lyo.Webhook.Twilio;

/// <summary>
/// Validates Twilio webhook requests using the auth token and <c>X-Twilio-Signature</c> (HMAC-SHA1, Base64). Use with <see cref="VerifiedWebhookEndpointBuilder" />; ensure
/// <see cref="WebhookVerificationContext.RequestUrl" /> is the public URL Twilio called and <see cref="WebhookVerificationContext.Parameters" /> contains form fields (set
/// automatically for <c>application/x-www-form-urlencoded</c> bodies by Lyo.Webhook).
/// </summary>
public sealed class TwilioWebhookSignatureVerifier : IWebhookSignatureVerifier
{
    private readonly string _authToken;

    /// <param name="authToken">Twilio account auth token (same secret used in Twilio Console).</param>
    public TwilioWebhookSignatureVerifier(string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken);
        _authToken = authToken;
    }

    /// <inheritdoc />
    public WebhookVerificationResult Verify(WebhookVerificationContext context)
    {
        if (!WebhookHeaders.TryGet(context.Headers, "X-Twilio-Signature", out var signature))
            return WebhookVerificationResult.Fail(WebhookVerificationFailureReason.MissingHeader);

        if (string.IsNullOrEmpty(context.RequestUrl))
            return WebhookVerificationResult.Fail(WebhookVerificationFailureReason.MissingParameter);

        var parameters = context.Parameters ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var urlWithPort = TwilioUrlNormalization.AddExplicitDefaultPort(context.RequestUrl);
        var urlWithoutPort = TwilioUrlNormalization.RemoveNonDefaultPort(context.RequestUrl);
        var expectedWithPort = ComputeSignature(urlWithPort, parameters);
        var expectedWithoutPort = ComputeSignature(urlWithoutPort, parameters);
        if (WebhookCrypto.FixedTimeEquals(signature, expectedWithPort) || WebhookCrypto.FixedTimeEquals(signature, expectedWithoutPort))
            return WebhookVerificationResult.Ok();

        return WebhookVerificationResult.Fail(WebhookVerificationFailureReason.InvalidSignature);
    }

    private string ComputeSignature(string url, IReadOnlyDictionary<string, string> parameters)
    {
        var builder = new StringBuilder(url);
        var keys = new List<string>(parameters.Keys);
        keys.Sort(StringComparer.Ordinal);
        foreach (var key in keys) {
            builder.Append(key);
            parameters.TryGetValue(key, out var value);
            builder.Append(value ?? string.Empty);
        }

        var keyBytes = Encoding.UTF8.GetBytes(_authToken);
        var dataBytes = Encoding.UTF8.GetBytes(builder.ToString());
        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hash);
    }
}