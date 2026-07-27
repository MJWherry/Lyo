namespace Lyo.Exceptions.Models;

/// <summary>Exception for an HTTP status code that has no dedicated exception type. Created via <see cref="HttpExceptions.FromStatusCode" />.</summary>
public sealed class GenericHttpException : HttpException
{
    /// <inheritdoc />
    /// <remarks>True for status codes conventionally worth retrying: 408, 429, 502, 503, and 504.</remarks>
    public override bool IsTransient => StatusCode is 408 or 429 or 502 or 503 or 504;

    /// <summary>Initializes a new instance of the <see cref="GenericHttpException" /> class.</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public GenericHttpException(int statusCode, string message, Exception? innerException = null)
        : base(statusCode, message, innerException) { }
}

/// <summary>Factory helpers for creating <see cref="HttpException" /> instances from raw HTTP status codes (e.g. when rethrowing server errors in API clients).</summary>
public static class HttpExceptions
{
    /// <summary>
    /// Creates the dedicated <see cref="HttpException" /> subclass for a known status code (400, 401, 403, 404, 409, 410, 422, 429, 503, 504), or a
    /// <see cref="GenericHttpException" /> for any other code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <returns>The best-matching <see cref="HttpException" /> for <paramref name="statusCode" />.</returns>
    public static HttpException FromStatusCode(int statusCode, string message, Exception? innerException = null)
        => statusCode switch {
            400 => new BadRequestException(message, innerException),
            401 => new UnauthorizedException(message, innerException),
            403 => new ForbiddenException(message, innerException),
            404 => new NotFoundException(message, innerException),
            409 => new ConflictException(message, innerException),
            410 => new GoneException(message, innerException),
            422 => new UnprocessableEntityException(message, innerException),
            429 => new RateLimitExceededException(message, innerException),
            503 => new ServiceUnavailableException(message, innerException),
            504 => new GatewayTimeoutException(message, innerException),
            var _ => new GenericHttpException(statusCode, message, innerException)
        };
}
