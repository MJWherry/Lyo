namespace Lyo.Webhook;

/// <summary>Outcome of a signature verification attempt.</summary>
public readonly struct WebhookVerificationResult
{
    public bool Success { get; }

    public WebhookVerificationFailureReason FailureReason { get; }

    private WebhookVerificationResult(bool success, WebhookVerificationFailureReason failureReason)
    {
        Success = success;
        FailureReason = failureReason;
    }

    public static WebhookVerificationResult Ok() => new(true, WebhookVerificationFailureReason.None);

    public static WebhookVerificationResult Fail(WebhookVerificationFailureReason reason) => new(false, reason);
}

/// <summary>Why verification failed. Provider libraries may document extra semantics.</summary>
public enum WebhookVerificationFailureReason
{
    None = 0,
    MissingHeader = 1,
    InvalidSignature = 2,
    MalformedHeader = 3,
    StaleTimestamp = 4,
    MissingParameter = 5,
    Unsupported = 6
}