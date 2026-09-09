namespace Lyo.Exceptions.Models;

/// <summary>HTTP 429 when a rate limit has been exceeded.</summary>
public class RateLimitExceededException : HttpException
{
    private const int HttpStatusCode = 429;

    /// <inheritdoc />
    public override bool IsTransient => true;

    /// <summary>Suggested wait before retry, when supplied.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Limit that was exceeded, when supplied.</summary>
    public int? RateLimit { get; }

    /// <summary>Window the limit applies to, when supplied.</summary>
    public TimeSpan? RateLimitWindow { get; }

    /// <summary>Mints a <see cref="RateLimitExceededException" /> with the default message.</summary>
    public RateLimitExceededException()
        : base(HttpStatusCode, "Rate limit has been exceeded.") { }

    /// <summary>Builds a <see cref="RateLimitExceededException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public RateLimitExceededException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public RateLimitExceededException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Builds a <see cref="RateLimitExceededException" /> that records limit details.</summary>
    /// <param name="retryAfter">Suggested wait before retry.</param>
    /// <param name="rateLimit">Limit that was exceeded.</param>
    /// <param name="rateLimitWindow">Window the limit applies to.</param>
    public RateLimitExceededException(TimeSpan? retryAfter = null, int? rateLimit = null, TimeSpan? rateLimitWindow = null)
        : base(HttpStatusCode, BuildMessage(retryAfter, rateLimit, rateLimitWindow))
    {
        RetryAfter = retryAfter;
        RateLimit = rateLimit;
        RateLimitWindow = rateLimitWindow;
    }

    /// <summary>Builds a <see cref="RateLimitExceededException" /> that records limit details and wraps <paramref name="innerException" />.</summary>
    /// <param name="retryAfter">Suggested wait before retry.</param>
    /// <param name="rateLimit">Limit that was exceeded.</param>
    /// <param name="rateLimitWindow">Window the limit applies to.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public RateLimitExceededException(TimeSpan? retryAfter, int? rateLimit, TimeSpan? rateLimitWindow, Exception? innerException)
        : base(HttpStatusCode, BuildMessage(retryAfter, rateLimit, rateLimitWindow), innerException)
    {
        RetryAfter = retryAfter;
        RateLimit = rateLimit;
        RateLimitWindow = rateLimitWindow;
    }

    private static string BuildMessage(TimeSpan? retryAfter, int? rateLimit, TimeSpan? rateLimitWindow)
    {
        var message = "Rate limit has been exceeded.";
        if (rateLimit.HasValue && rateLimitWindow.HasValue)
            message += $" Limit: {rateLimit.Value} requests per {rateLimitWindow.Value.TotalSeconds} seconds.";
        else if (rateLimit.HasValue)
            message += $" Limit: {rateLimit.Value} requests.";

        if (retryAfter.HasValue)
            message += $" Please retry after {retryAfter.Value.TotalSeconds} seconds.";

        return message;
    }
}