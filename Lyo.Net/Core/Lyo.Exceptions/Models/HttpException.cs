namespace Lyo.Exceptions.Models;

/// <summary>Base exception for HTTP-related errors. Provides a StatusCode for consistent handling in middleware and API layers.</summary>
public abstract class HttpException : Exception
{
    /// <summary>Gets the HTTP status code associated with this exception (e.g. 404, 403, 409).</summary>
    public int StatusCode { get; }

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