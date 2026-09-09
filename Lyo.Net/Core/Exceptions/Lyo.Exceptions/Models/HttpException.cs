namespace Lyo.Exceptions.Models;

/// <summary>Base type for HTTP errors. Carries a StatusCode so middleware and API layers can handle failures uniformly.</summary>
public abstract class HttpException : Exception
{
    /// <summary>HTTP status for this failure (for example 404, 403, 409).</summary>
    public int StatusCode { get; }

    /// <summary>
    /// Optional stable, machine-readable error code (for example <c>"user.not_found"</c>) that API consumers can switch on instead of parsing message text. Set via object
    /// initializer: <c>new NotFoundException("...") { ErrorCode = "user.not_found" }</c>.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// True when the failure is transient and a retry may succeed (for example rate limiting, temporary unavailability). Defaults to false; transient exception
    /// types override this to return true so retry policies can use a single predicate.
    /// </summary>
    public virtual bool IsTransient => false;

    /// <summary>Builds an <see cref="HttpException" /> for <paramref name="statusCode" />.</summary>
    /// <param name="statusCode">HTTP status (for example 404, 403, 409).</param>
    /// <param name="message">Error text.</param>
    protected HttpException(int statusCode, string message)
        : base(message)
        => StatusCode = statusCode;

    /// <summary>Wraps <paramref name="innerException" /> with <paramref name="statusCode" /> and <paramref name="message" />.</summary>
    /// <param name="statusCode">HTTP status.</param>
    /// <param name="message">Error text.</param>
    /// <param name="innerException">Cause of this exception.</param>
    protected HttpException(int statusCode, string message, Exception? innerException)
        : base(message, innerException)
        => StatusCode = statusCode;
}