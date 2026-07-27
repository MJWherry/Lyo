namespace Lyo.Exceptions.Models;

/// <summary>Base exception for HTTP-related errors. Provides a StatusCode for consistent handling in middleware and API layers.</summary>
public abstract class HttpException : Exception
{
    /// <summary>Gets the HTTP status code associated with this exception (e.g. 404, 403, 409).</summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets an optional stable, machine-readable error code (e.g. <c>"user.not_found"</c>) that API consumers can switch on instead of parsing message text. Set via object
    /// initializer: <c>new NotFoundException("...") { ErrorCode = "user.not_found" }</c>.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets whether the failure is transient and the operation may succeed if retried (e.g. rate limiting, temporary unavailability). Defaults to false; transient exception
    /// types override this to return true so retry policies can use a single predicate.
    /// </summary>
    public virtual bool IsTransient => false;

    /// <summary>Initializes a new instance of the <see cref="HttpException" /> class.</summary>
    /// <param name="statusCode">The HTTP status code (e.g. 404, 403, 409).</param>
    /// <param name="message">The message that describes the error.</param>
    protected HttpException(int statusCode, string message)
        : base(message)
        => StatusCode = statusCode;

    /// <summary>Initializes a new instance of the <see cref="HttpException" /> class.</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected HttpException(int statusCode, string message, Exception? innerException)
        : base(message, innerException)
        => StatusCode = statusCode;
}