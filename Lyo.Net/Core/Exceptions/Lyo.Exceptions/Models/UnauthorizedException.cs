namespace Lyo.Exceptions.Models;

/// <summary>HTTP 401 when authentication is missing or invalid.</summary>
public class UnauthorizedException : HttpException
{
    private const int HttpStatusCode = 401;

    /// <summary>Why authentication failed, when supplied.</summary>
    public string? Reason { get; }

    /// <summary>Mints an <see cref="UnauthorizedException" /> with the default message.</summary>
    public UnauthorizedException()
        : base(HttpStatusCode, "Authentication is required.") { }

    /// <summary>Builds an <see cref="UnauthorizedException" /> using <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    public UnauthorizedException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="message" />.</summary>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    public UnauthorizedException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Builds an <see cref="UnauthorizedException" /> that records <paramref name="reason" />.</summary>
    /// <param name="reason">Why authentication failed.</param>
    /// <param name="includeReasonInMessage">When true, appends the reason to the exception message. Default is true.</param>
    public UnauthorizedException(string reason, bool includeReasonInMessage = true)
        : base(HttpStatusCode, includeReasonInMessage ? $"Authentication is required. Reason: {reason}" : "Authentication is required.")
        => Reason = reason;
}