using System.Security.Cryptography;
using System.Text;

namespace Lyo.Webhook.Twilio.Tests;

public sealed class TwilioWebhookSignatureVerifierTests
{
    [Fact]
    public void Verify_accepts_matching_signature()
    {
        const string authToken = "my-auth-token";
        const string url = "https://example.com/webhook/twilio";
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["AccountSid"] = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", ["MessageSid"] = "SMbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
        };

        var expected = ComputeTwilioSignature(authToken, TwilioUrlNormalization.AddExplicitDefaultPort(url), parameters);
        var verifier = new TwilioWebhookSignatureVerifier(authToken);
        var context =
            new WebhookVerificationContext(Array.Empty<byte>(), new Dictionary<string, string> { ["X-Twilio-Signature"] = expected }) { RequestUrl = url, Parameters = parameters };

        var result = verifier.Verify(context);
        Assert.True(result.Success);
    }

    [Fact]
    public void Verify_rejects_wrong_token()
    {
        const string url = "https://example.com/webhook/twilio";
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal) { ["k"] = "v" };
        var sig = ComputeTwilioSignature("correct-token", TwilioUrlNormalization.AddExplicitDefaultPort(url), parameters);
        var verifier = new TwilioWebhookSignatureVerifier("wrong-token");
        var context =
            new WebhookVerificationContext(Array.Empty<byte>(), new Dictionary<string, string> { ["X-Twilio-Signature"] = sig }) { RequestUrl = url, Parameters = parameters };

        Assert.False(verifier.Verify(context).Success);
    }

    private static string ComputeTwilioSignature(string authToken, string url, IReadOnlyDictionary<string, string> parameters)
    {
        var builder = new StringBuilder(url);
        var keys = new List<string>(parameters.Keys);
        keys.Sort(StringComparer.Ordinal);
        foreach (var key in keys) {
            builder.Append(key);
            parameters.TryGetValue(key, out var value);
            builder.Append(value ?? string.Empty);
        }

        using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken)))
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}