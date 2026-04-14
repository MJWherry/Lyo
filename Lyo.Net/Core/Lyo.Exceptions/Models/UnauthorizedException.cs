namespace Lyo.Exceptions.Models;

/// <summary>Exception thrown when authentication is required but not provided or invalid. Maps to HTTP 401.</summary>
public class UnauthorizedException : HttpException
{
    private const int HttpStatusCode = 401;

    /// <summary>Gets the reason for the unauthorized access, if provided.</summary>
    public string? Reason { get; }

    /// <summary>Initializes a new instance of the <see cref="UnauthorizedException" /> class.</summary>
    public UnauthorizedException()
        : base(HttpStatusCode, "Authentication is required.") { }

    /// <summary>Initializes a new instance of the <see cref="UnauthorizedException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public UnauthorizedException(string message)
        : base(HttpStatusCode, message) { }

    /// <summary>Initializes a new instance of the <see cref="UnauthorizedException" /> class with a specified error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnauthorizedException(string message, Exception? innerException)
        : base(HttpStatusCode, message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="UnauthorizedException" /> class with a reason.</summary>
    /// <param name="reason">The reason for the unauthorized access.</param>
    public UnauthorizedException(string reason, bool includeReasonInMessage = true)
        : base(HttpStatusCode, includeReasonInMessage ? $"Authentication is required. Reason: {reason}" : "Authentication is required.")
        => Reason = reason;
}