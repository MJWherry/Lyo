namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when a rate limit has been exceeded. Maps to HTTP 429.</summary>
public class RateLimitExceededException : HttpException
{
    private const int HttpStatusCode = 429;

    /// <summary>Gets the retry after time, if provided.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Gets the rate limit that was exceeded, if provided.</summary>
    public int? RateLimit { get; }

    /// <summary>Gets the time window for the rate limit, if provided.</summary>
    public TimeSpan? RateLimitWindow { get; }

    /// <summary>Initializes a new instance of the <see cref="RateLimitExceededException" /> class.</summary>
    public RateLimitExceededException()
        : base(HttpStatusCode, "Rate limit has been exceeded.") { }

    /// <summary>Initializes a new instance of the <see cref="RateLimitExceededException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public RateLimitExceededException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="RateLimitExceededException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RateLimitExceededException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="RateLimitExceededException" /> class with rate limit information.</summary>
    /// <param name="retryAfter">The suggested retry after time.</param>
    /// <param name="rateLimit">The rate limit that was exceeded.</param>
    /// <param name="rateLimitWindow">The time window for the rate limit.</param>
    public RateLimitExceededException(TimeSpan? retryAfter = null, int? rateLimit = null, TimeSpan? rateLimitWindow = null)
        : base(HttpStatusCode, BuildMessage(retryAfter, rateLimit, rateLimitWindow))
    {
        RetryAfter = retryAfter;
        RateLimit = rateLimit;
        RateLimitWindow = rateLimitWindow;
    }

    /// <summary>Initializes a new instance of the <see cref="RateLimitExceededException" /> class with rate limit information and inner exception.</summary>
    /// <param name="retryAfter">The suggested retry after time.</param>
    /// <param name="rateLimit">The rate limit that was exceeded.</param>
    /// <param name="rateLimitWindow">The time window for the rate limit.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
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